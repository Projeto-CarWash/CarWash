using System.Text.Json;
using CarWash.Application.Common.Exceptions;
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
#pragma warning disable S1075 // URI base padronizada para o campo `type` do ProblemDetails (RFC 7807) — não é endpoint real.
    private const string BaseTypeUrl = "https://carwash/errors/";
#pragma warning restore S1075

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
        catch (BadHttpRequestException ex)
        {
            // Falha de binding do framework (Guid malformado, JSON inválido, etc.).
            // Em Minimal APIs o framework responde 400 antes; este catch é defesa em profundidade.
            await EscreverProblemAsync(
                context,
                status: StatusCodes.Status400BadRequest,
                slug: "invalid-request",
                title: "Identificador inválido.",
                erros: new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["request"] = [ex.Message],
                }).ConfigureAwait(false);
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

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var payload = JsonSerializer.Serialize(problem, JsonOptions);
        await context.Response.WriteAsync(payload).ConfigureAwait(false);
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
