using System.Text.Json;
using System.Text.RegularExpressions;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Exceptions;
using CarWash.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace CarWash.Api.Middleware;

/// <summary>
/// Traduz exceções da pipeline em <see cref="ProblemDetails"/> (RFC 7807) com
/// o slug, status e mensagem corretos. Garante <c>correlationId</c> em todas as
/// respostas de erro. Stack traces nunca vazam (status 500 → mensagem genérica).
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    public const string MensagemErroInterno = "Não foi possível concluir a operação no momento. Tente novamente.";
    public const string MensagemIdentificadorInvalido = "Identificador inválido.";
    public const string MensagemCorpoInvalido = "Corpo da requisição inválido. Verifique o JSON e tente novamente.";
    public const string MensagemCampoInvalido = "Valor inválido para o campo informado.";
    public const string MensagemPathParametroInvalido = "Valor inválido para o parâmetro da rota.";

#pragma warning disable S1075 // URI base padronizada para o campo `type` do ProblemDetails (RFC 7807) — não é endpoint real.
    private const string BaseTypeUrl = "https://carwash/errors/";
#pragma warning restore S1075

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // Padrões de mensagem do framework (Minimal API binder) — usados para diferenciar
    // erro de body vs erro de path/query e extrair o nome do parâmetro de rota.
    private static readonly Regex RegexBindParameter = new(
        @"Failed to bind parameter ""(?<tipo>[^""\s]+)\s+(?<nome>[^""]+)"" from ""(?<valor>[^""]*)""\.?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(50));

    private static readonly Regex RegexReadBody = new(
        @"Failed to read parameter ""(?<tipo>[^""\s]+)\s+(?<nome>[^""]+)"" from the request body as JSON\.?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(50));

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _log;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> log)
    {
        _next = next;
        _log = log;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (ApiException ex)
        {
            await EscreverProblemAsync(
                context,
                status: ex.StatusCode,
                slug: ex.ErrorCode.ToLowerInvariant().Replace('_', '-'),
                title: ex.Message,
                erros: ex.Errors).ConfigureAwait(false);
        }
        catch (ValidationException ex)
        {
            await EscreverProblemAsync(
                context,
                status: StatusCodes.Status400BadRequest,
                slug: "validation-error",
                title: ex.Message,
                erros: ex.Erros).ConfigureAwait(false);
        }
        catch (InvalidCredentialsException ex)
        {
            await EscreverProblemAsync(
                context,
                status: StatusCodes.Status401Unauthorized,
                slug: "invalid-credentials",
                title: ex.Message,
                erros: null).ConfigureAwait(false);
        }
        catch (RefreshTokenInvalidoException ex)
        {
            // Cookie de refresh ausente / expirado / revogado. Cliente deve
            // redirecionar para /login. Cookie é apagado pelo endpoint que conhece o nome.
            await EscreverProblemAsync(
                context,
                status: StatusCodes.Status401Unauthorized,
                slug: "refresh-token-invalido",
                title: ex.Message,
                erros: null).ConfigureAwait(false);
        }
        catch (UsuarioInativoException ex)
        {
            await EscreverProblemAsync(
                context,
                status: StatusCodes.Status403Forbidden,
                slug: "usuario-inativo",
                title: ex.Message,
                erros: null).ConfigureAwait(false);
        }
        catch (UsuarioBloqueadoException ex)
        {
            var bloqueadoAteUtc = ex.BloqueadoAte.ToUniversalTime();
            var segundosRestantes = Math.Max(0, (int)Math.Ceiling((bloqueadoAteUtc - DateTime.UtcNow).TotalSeconds));

            // Retry-After header (RFC 7231) — segundos para o cliente poder
            // mostrar contagem regressiva e/ou bloquear retries automáticos.
            if (!context.Response.HasStarted)
            {
                context.Response.Headers["Retry-After"] = segundosRestantes.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            await EscreverProblemAsync(
                context,
                status: StatusCodes.Status403Forbidden,
                slug: "usuario-bloqueado",
                title: ex.Message,
                erros: null,
                extensions: new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["bloqueadoAte"] = bloqueadoAteUtc.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
                    ["retryAfterSeconds"] = segundosRestantes,
                }).ConfigureAwait(false);
        }
        catch (RecursoInativoException ex)
        {
            // Requisição sintaticamente válida, mas referencia um recurso inativo
            // (filial/veículo/cliente/serviço) — 422 Unprocessable Entity.
            await EscreverProblemAsync(
                context,
                status: StatusCodes.Status422UnprocessableEntity,
                slug: RecursoInativoException.SlugPadrao,
                title: ex.Message,
                erros: null).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            await EscreverProblemAsync(
                context,
                status: StatusCodes.Status400BadRequest,
                slug: "domain-rule",
                title: ex.Message,
                erros: null).ConfigureAwait(false);
        }
        catch (ConflictException ex)
        {
            await EscreverProblemAsync(
                context,
                status: StatusCodes.Status409Conflict,
                slug: ex.Slug,
                title: ex.Message,
                erros: null).ConfigureAwait(false);
        }
        catch (NotFoundException ex)
        {
            await EscreverProblemAsync(
                context,
                status: StatusCodes.Status404NotFound,
                slug: "not-found",
                title: ex.Message,
                erros: null).ConfigureAwait(false);
        }
        catch (SessaoConfirmacaoExpiradaException ex)
        {
            // RF015: token de confirmação válido mas expirado — o recurso (sessão
            // de confirmação) deixou de existir. 410 Gone para o cliente saber que
            // deve gerar uma nova pré-confirmação em vez de apenas reenviar.
            await EscreverProblemAsync(
                context,
                status: StatusCodes.Status410Gone,
                slug: SessaoConfirmacaoExpiradaException.Slug,
                title: ex.Message,
                erros: null).ConfigureAwait(false);
        }
        catch (BadHttpRequestException ex)
        {
            // Falha de binding do framework — diferenciar:
            //   • body deserialization (JSON malformado, enum desconhecido, null em campo
            //     não-nullable) → `Corpo da requisição inválido.` + chave `body` ou nome
            //     do campo extraído de JsonException.Path (`$.perfil` → `perfil`).
            //   • path/query binding (Guid malformado em `{id:guid}`) → mantém
            //     `Identificador inválido.` + chave com o nome do parâmetro (`id`).
            // NUNCA vazar o nome do parâmetro C# (`LoginCommand command`, `Guid id`).
            var (title, erros) = ClassificarBadRequest(ex);
            await EscreverProblemAsync(
                context,
                status: StatusCodes.Status400BadRequest,
                slug: "invalid-request",
                title: title,
                erros: erros).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // último resort: log + 500 genérico.
        catch (Exception ex)
        {
            var correlationId = ResolverCorrelationId(context);
            _log.LogError(ex, "Falha não tratada. CorrelationId={CorrelationId}", correlationId);
            await EscreverProblemAsync(
                context,
                status: StatusCodes.Status500InternalServerError,
                slug: "internal-error",
                title: MensagemErroInterno,
                erros: null).ConfigureAwait(false);
        }
#pragma warning restore CA1031
    }

    private static async Task EscreverProblemAsync(
        HttpContext context,
        int status,
        string slug,
        string title,
        IReadOnlyDictionary<string, string[]>? erros,
        IReadOnlyDictionary<string, object?>? extensions = null)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        var correlationId = ResolverCorrelationId(context);

        var problem = new ProblemDetails
        {
            Type = BaseTypeUrl + slug,
            Title = title,
            Status = status,
        };
        problem.Extensions["correlationId"] = correlationId;
        if (erros is { Count: > 0 })
        {
            problem.Extensions["errors"] = erros;
        }

        if (extensions is { Count: > 0 })
        {
            foreach (var kv in extensions)
            {
                problem.Extensions[kv.Key] = kv.Value;
            }
        }

        // Preserva headers de segurança/cache que o endpoint definiu ANTES da
        // exceção. `Response.Clear()` apaga headers e body — re-aplicamos os que
        // são parte do contrato do endpoint (ex.: `Cache-Control: no-store` em
        // /auth/*) e o `Retry-After` já setado para 403 de lockout.
        var cacheControl = context.Response.Headers.CacheControl.ToString();
        var retryAfter = context.Response.Headers["Retry-After"].ToString();

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        if (!string.IsNullOrEmpty(cacheControl))
        {
            context.Response.Headers.CacheControl = cacheControl;
        }

        if (!string.IsNullOrEmpty(retryAfter))
        {
            context.Response.Headers["Retry-After"] = retryAfter;
        }

        var payload = JsonSerializer.Serialize(problem, JsonOptions);
        await context.Response.WriteAsync(payload).ConfigureAwait(false);
    }

    /// <summary>
    /// Classifica uma <see cref="BadHttpRequestException"/> entre erro de body ou
    /// de parâmetro de rota. Devolve o <c>title</c> apropriado e o dicionário
    /// <c>errors</c> sem expor identificadores internos (tipos/parâmetros C#).
    /// </summary>
    internal static (string Title, IReadOnlyDictionary<string, string[]> Erros) ClassificarBadRequest(BadHttpRequestException ex)
    {
        ArgumentNullException.ThrowIfNull(ex);

        var mensagem = ex.Message ?? string.Empty;

        // 1) Erro de path/query: "Failed to bind parameter "Guid id" from "abc"."
        var matchBind = RegexBindParameter.Match(mensagem);
        if (matchBind.Success)
        {
            var nomeParam = matchBind.Groups["nome"].Value;
            var chave = NormalizarChaveParametro(nomeParam);
            var tipoLower = matchBind.Groups["tipo"].Value.ToLowerInvariant();
            var ehGuid = tipoLower.Contains("guid", StringComparison.Ordinal);

            return (
                ehGuid ? MensagemIdentificadorInvalido : MensagemPathParametroInvalido,
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    [chave] = [ehGuid
                        ? "Identificador deve ser um GUID válido."
                        : "Valor do parâmetro de rota é inválido."],
                });
        }

        // 2) Erro de body deserialization: "Failed to read parameter "X command" from the request body as JSON."
        if (RegexReadBody.IsMatch(mensagem))
        {
            var (campo, detalhe) = ExtrairCampoDeJsonException(ex.InnerException);

            return (MensagemCorpoInvalido, new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                [campo] = [detalhe],
            });
        }

        // 3) Fallback genérico — mensagem do framework não bate com os padrões
        // conhecidos. Resposta neutra, sem vazar o conteúdo bruto.
        return (MensagemCorpoInvalido, new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["body"] = ["Requisição inválida."],
        });
    }

    private static string NormalizarChaveParametro(string nomeParametroC)
    {
        // "id" → "id"; "Id" → "id"; "usuarioId" → "usuarioId" (mantém camelCase).
        if (string.IsNullOrWhiteSpace(nomeParametroC))
        {
            return "request";
        }

        var trimmed = nomeParametroC.Trim();
        return char.ToLowerInvariant(trimmed[0]) + trimmed[1..];
    }

    /// <summary>
    /// Extrai o nome do campo JSON do <c>Path</c> de uma <see cref="JsonException"/>
    /// (ex.: <c>$.perfil</c> → <c>perfil</c>). Quando o path é raiz/ausente, devolve
    /// <c>body</c> com mensagem genérica em PT-BR.
    /// </summary>
    private static (string Campo, string Detalhe) ExtrairCampoDeJsonException(Exception? inner)
    {
        if (inner is not JsonException json)
        {
            return ("body", "Corpo da requisição inválido. Verifique o JSON e tente novamente.");
        }

        var path = json.Path;
        if (string.IsNullOrWhiteSpace(path) || path is "$" or "$.")
        {
            return ("body", "Corpo da requisição inválido. Verifique o JSON e tente novamente.");
        }

        // Path típico: "$.perfil", "$.itens[0].quantidade".
        var campo = path.StartsWith("$.", StringComparison.Ordinal) ? path[2..] : path;

        // Mantém apenas o nome até o primeiro `.` ou `[` — chave do campo de topo
        // mais relevante para o cliente saber onde corrigir.
        var corte = campo.IndexOfAny(['.', '[']);
        if (corte > 0)
        {
            campo = campo[..corte];
        }

        if (string.IsNullOrWhiteSpace(campo))
        {
            return ("body", "Corpo da requisição inválido. Verifique o JSON e tente novamente.");
        }

        return (campo, MensagemCampoInvalido);
    }

    private static string ResolverCorrelationId(HttpContext context)
    {
        if (context.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var raw)
            && raw is string id
            && !string.IsNullOrWhiteSpace(id))
        {
            return id;
        }

        return Guid.NewGuid().ToString("N");
    }
}
