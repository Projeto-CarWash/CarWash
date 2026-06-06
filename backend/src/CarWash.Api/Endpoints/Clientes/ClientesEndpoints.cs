using CarWash.Api.Filters;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Clientes.AlterarStatus;
using CarWash.Application.Clientes.Atualizar;
using CarWash.Application.Clientes.Common;
using CarWash.Application.Clientes.Criar;
using CarWash.Application.Clientes.Listar;
using CarWash.Application.Clientes.ObterPorId;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using ValidationException = CarWash.Application.Common.Exceptions.ValidationException;

namespace CarWash.Api.Endpoints.Clientes;

/// <summary>
/// Endpoints de clientes (RF002 + RF003). Mantém os contratos HTTP herdados do
/// antigo <c>ClientesController</c>: <c>Cache-Control: no-store</c> em rotas com
/// PII, <c>NotFoundException</c> → 404 canônico, validação inline de paginação.
/// </summary>
public static class ClientesEndpoints
{
    private const int TamanhoPaginaMaximo = 100;
    private const string MensagemPayloadInvalido =
        "Dados inválidos. Verifique os campos e tente novamente.";

    public static IEndpointRouteBuilder MapClientes(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var grupo = app.MapGroup("/api/v1/clientes")
            .WithTags("Clientes")
            .RequireAuthorization();

        grupo.MapPost("/", CriarAsync)
            .WithName("CriarCliente")
            .Produces<CriarClienteResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict);

        grupo.MapGet("/", ListarAsync)
            .WithName("ListarClientes")
            .Produces<ListaClientesResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        grupo.MapGet("/{id:guid}", ObterPorIdAsync)
            .WithName("ObterClientePorId")
            .Produces<ClienteResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        grupo.MapPut("/{id:guid}", AtualizarAsync)
            .WithName("AtualizarCliente")
            .Produces<ClienteResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        grupo.MapPatch("/{id:guid}/status", AlterarStatusAsync)
            .WithName("AlterarStatusCliente")
            .Produces<ClienteResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<Created<CriarClienteResponse>> CriarAsync(
        [FromBody] CriarClienteRequest? request,
        [FromServices] ICommandHandler<CriarClienteCommand, CriarClienteResponse> handler,
        [FromServices] IValidator<CriarClienteCommand> validator,
        [FromServices] ILogger<CriarClienteEndpointMarker> logger,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw ValidationProblems.BodyAusente(
                "Dados do cliente inválidos. Verifique os campos e tente novamente.",
                "Corpo da requisição ausente ou malformado.");
        }

        string traceId = http.TraceIdentifier;
        var usuarioId = ObterUsuarioId(http);

        var command = new CriarClienteCommand(
            Nome: request.Nome,
            DataNascimento: request.DataNascimento,
            Cpf: request.Cpf,
            Cnpj: request.Cnpj,
            Telefone: request.Telefone,
            Celular: request.Celular,
            Email: request.Email,
            Endereco: request.Endereco,
            Veiculos: request.Veiculos,
            Observacoes: request.Observacoes,
            TraceId: traceId,
            UsuarioId: usuarioId);

        await ValidarAsync(validator, command, "Dados do cliente inválidos. Verifique os campos e tente novamente.", cancellationToken)
            .ConfigureAwait(false);

        var resposta = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Cliente cadastrado com sucesso. TraceId: {TraceId}. ClienteId: {ClienteId}. UsuarioId: {UsuarioId}",
            traceId,
            resposta.Id,
            usuarioId);

        return TypedResults.Created($"/api/v1/clientes/{resposta.Id}", resposta);
    }

    private static async Task<Ok<ListaClientesResponse>> ListarAsync(
        [FromQuery] string? busca,
        [FromQuery] bool? ativo,
        [FromServices] IQueryHandler<ListarClientesQuery, ListaClientesResponse> handler,
        HttpContext http,
        CancellationToken cancellationToken,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20)
    {
        // GAP-PAG-0 + GAP-CLAMP: validação explícita de paginação. Antes a página
        // <= 0 e o tamanho fora da faixa eram normalizados silenciosamente, levando
        // o cliente a achar que o filtro estava sendo respeitado.
        var erros = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        if (pagina < 1)
        {
            erros["pagina"] = ["Página deve ser maior ou igual a 1."];
        }

        if (tamanhoPagina < 1)
        {
            erros["tamanhoPagina"] = ["Tamanho da página deve ser maior ou igual a 1."];
        }
        else if (tamanhoPagina > TamanhoPaginaMaximo)
        {
            erros["tamanhoPagina"] = [$"Tamanho da página deve ser no máximo {TamanhoPaginaMaximo}."];
        }

        if (erros.Count > 0)
        {
            throw new ValidationException(
                "Parâmetros de paginação inválidos. Verifique os campos e tente novamente.",
                erros);
        }

        // BUG-CACHE-PII: lista expõe PII (nome, e-mail, telefone) — proibir cache.
        http.Response.Headers[HeaderNames.CacheControl] = "no-store";

        var resposta = await handler.HandleAsync(
            new ListarClientesQuery(busca, ativo, pagina, tamanhoPagina),
            cancellationToken).ConfigureAwait(false);

        return TypedResults.Ok(resposta);
    }

    private static async Task<Ok<ClienteResponse>> ObterPorIdAsync(
        Guid id,
        [FromServices] IQueryHandler<ObterClientePorIdQuery, ClienteResponse> handler,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        // BUG-CACHE-PII: detalhe contém PII completa — proibir cache em proxies/browser.
        http.Response.Headers[HeaderNames.CacheControl] = "no-store";

        var resposta = await handler.HandleAsync(new ObterClientePorIdQuery(id), cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(resposta);
    }

    private static async Task<Ok<ClienteResponse>> AtualizarAsync(
        Guid id,
        [FromBody] AtualizarClienteRequest? request,
        [FromServices] ICommandHandler<AtualizarClienteCommand, ClienteResponse> handler,
        [FromServices] IValidator<AtualizarClienteCommand> validator,
        [FromServices] ILogger<AtualizarClienteEndpointMarker> logger,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw ValidationProblems.BodyAusente(
                "Dados do cliente inválidos. Verifique os campos e tente novamente.",
                "Corpo da requisição ausente ou malformado.");
        }

        var usuarioId = ObterUsuarioId(http);
        var command = new AtualizarClienteCommand(
            Id: id,
            Nome: request.Nome,
            DataNascimento: request.DataNascimento,
            Telefone: request.Telefone,
            Celular: request.Celular,
            Email: request.Email,
            Endereco: request.Endereco,
            CamposExtras: request.CamposExtras,
            UsuarioId: usuarioId);

        await ValidarAsync(validator, command, "Dados do cliente inválidos. Verifique os campos e tente novamente.", cancellationToken)
            .ConfigureAwait(false);

        var resposta = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Cliente atualizado. ClienteId: {ClienteId}. UsuarioId: {UsuarioId}", id, usuarioId);

        return TypedResults.Ok(resposta);
    }

    private static async Task<Ok<ClienteResponse>> AlterarStatusAsync(
        Guid id,
        [FromBody] AlterarStatusClienteRequest? request,
        [FromServices] ICommandHandler<AlterarStatusClienteCommand, ClienteResponse> handler,
        [FromServices] IValidator<AlterarStatusClienteCommand> validator,
        [FromServices] ILogger<AlterarStatusClienteEndpointMarker> logger,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // GAP-CW-CLI-STA-EMP: body {} ou ausência do campo "ativo" precisa
        // falhar com 400 — o tipo é bool? para distinguir "não informado"
        // de "false". Antes o framework caía em default(bool)=false silencioso.
        // ValidationProblems não cobre esse caso porque a chave aqui é "ativo"
        // (não "body") — mensagem específica que sinaliza o campo ausente.
        if (request.Ativo is null)
        {
            throw new ValidationException(
                MensagemPayloadInvalido,
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["ativo"] = ["Campo 'ativo' é obrigatório."],
                });
        }

        var usuarioId = ObterUsuarioId(http);
        var command = new AlterarStatusClienteCommand(id, request.Ativo, usuarioId);

        await ValidarAsync(validator, command, MensagemPayloadInvalido, cancellationToken).ConfigureAwait(false);

        var resposta = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Status do cliente alterado. ClienteId: {ClienteId}. Ativo: {Ativo}. UsuarioId: {UsuarioId}",
            id,
            request.Ativo.Value,
            usuarioId);

        return TypedResults.Ok(resposta);
    }

    private static async Task ValidarAsync<T>(
        IValidator<T> validator,
        T instancia,
        string mensagem,
        CancellationToken cancellationToken)
    {
        var resultado = await validator.ValidateAsync(instancia, cancellationToken).ConfigureAwait(false);
        ValidationProblems.EnsureValid(resultado, mensagem);
    }

    private static Guid? ObterUsuarioId(HttpContext http)
    {
        string? sub = http.User.FindFirst("sub")?.Value
            ?? http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

#pragma warning disable S2094, SA1502, CA1812 // Marcadores apenas para tipar o ILogger por endpoint (categoria de log estável).

    /// <summary>Marker usado em <c>ILogger&lt;T&gt;</c> para categoria de log estável.</summary>
    internal sealed class CriarClienteEndpointMarker
    {
    }

    /// <summary>Marker usado em <c>ILogger&lt;T&gt;</c> para categoria de log estável.</summary>
    internal sealed class AtualizarClienteEndpointMarker
    {
    }

    /// <summary>Marker usado em <c>ILogger&lt;T&gt;</c> para categoria de log estável.</summary>
    internal sealed class AlterarStatusClienteEndpointMarker
    {
    }

#pragma warning restore S2094, SA1502, CA1812
}
