using CarWash.Api.Filters;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Filiais.AlterarCelulasAtivas;
using CarWash.Application.Filiais.AlterarStatus;
using CarWash.Application.Filiais.Common;
using CarWash.Application.Filiais.Criar;
using CarWash.Application.Filiais.Listar;
using CarWash.Application.Filiais.ObterFilialPorId;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ValidationException = CarWash.Application.Common.Exceptions.ValidationException;

namespace CarWash.Api.Endpoints.Filiais;

/// <summary>
/// Endpoints de filiais (RF017 + RF018). Aplicam o contrato HTTP do ADR-0007
/// §4: envelope {id, mensagem, traceId} no POST, listagem paginada compatível
/// com <c>frontend/src/types/filial.ts</c>, leitura por id e ajuste de células
/// ativas (RF018). Grupo inteiro requer autenticação simples
/// (<c>RequireAuthorization()</c> puro — sem policy Admin; 403 fica para
/// RF-FUT003).
/// </summary>
public static class FiliaisEndpoints
{
    private const int TamanhoPaginaMaximo = 100;
    private const string MensagemPayloadInvalido =
        "Dados da filial inválidos. Verifique os campos e tente novamente.";

    public static IEndpointRouteBuilder MapFiliais(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var grupo = app.MapGroup("/api/v1/filiais")
            .WithTags("Filiais")
            .RequireAuthorization();

        grupo.MapPost("/", CriarAsync)
            .WithName("CriarFilial")
            .Produces<CriarFilialResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        grupo.MapGet("/", ListarAsync)
            .WithName("ListarFiliais")
            .Produces<ListaFiliaisResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        // GET /api/v1/filiais/{id} — leitura por id (RF017/RF018). Autenticação simples.
        grupo.MapGet("/{id:guid}", ObterPorIdAsync)
            .WithName("ObterFilialPorId")
            .Produces<FilialResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // PATCH /api/v1/filiais/{id}/celulas-ativas — ajuste de capacidade (RF018).
        // Autenticação simples (sem policy Admin; 403 fica para RF-FUT003).
        grupo.MapPatch("/{id:guid}/celulas-ativas", AlterarCelulasAtivasAsync)
            .WithName("AlterarCelulasAtivasFilial")
            .Produces<FilialResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        // PATCH /api/v1/filiais/{id}/status — ativar/inativar filial (RF017).
        // Filial inativa não aceita novos agendamentos (RF019 → 409 filial-inativa).
        grupo.MapPatch("/{id:guid}/status", AlterarStatusAsync)
            .WithName("AlterarStatusFilial")
            .Produces<FilialResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<Created<CriarFilialResponse>> CriarAsync(
        [FromBody] CriarFilialRequest? request,
        [FromServices] ICommandHandler<CriarFilialCommand, CriarFilialResponse> handler,
        [FromServices] IValidator<CriarFilialCommand> validator,
        [FromServices] ILogger<CriarFilialEndpointMarker> logger,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw ValidationProblems.BodyAusente(
                MensagemPayloadInvalido,
                "Corpo da requisição ausente ou malformado.");
        }

        string traceId = http.TraceIdentifier;
        var usuarioId = ObterUsuarioId(http);

        var command = new CriarFilialCommand(
            Nome: request.Nome,
            Codigo: request.Codigo,
            Cnpj: request.Cnpj,
            CelulasAtivas: request.CelulasAtivas,
            Timezone: request.Timezone,
            Endereco: request.Endereco,
            TraceId: traceId,
            UsuarioId: usuarioId);

        await ValidarAsync(validator, command, MensagemPayloadInvalido, cancellationToken).ConfigureAwait(false);

        var resposta = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        // Log estruturado — TraceId, FilialId, Codigo, UsuarioId. CNPJ
        // intencionalmente fora dos logs (PII fiscal — ADR-0007 §6.4).
        logger.LogInformation(
            "Filial cadastrada. TraceId: {TraceId}. FilialId: {FilialId}. Codigo: {Codigo}. UsuarioId: {UsuarioId}",
            traceId,
            resposta.Id,
            command.Codigo,
            usuarioId);

        return TypedResults.Created($"/api/v1/filiais/{resposta.Id}", resposta);
    }

    private static async Task<Ok<ListaFiliaisResponse>> ListarAsync(
        [FromQuery] string? busca,
        [FromQuery] bool? ativo,
        [FromServices] IQueryHandler<ListarFiliaisQuery, ListaFiliaisResponse> handler,
        CancellationToken cancellationToken,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20)
    {
        // Validação explícita de paginação (mesmo padrão de ClientesEndpoints).
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

        var resposta = await handler.HandleAsync(
            new ListarFiliaisQuery(busca, ativo, pagina, tamanhoPagina),
            cancellationToken).ConfigureAwait(false);

        return TypedResults.Ok(resposta);
    }

    private static async Task<Ok<FilialResponse>> ObterPorIdAsync(
        Guid id,
        [FromServices] IQueryHandler<ObterFilialPorIdQuery, FilialResponse> handler,
        CancellationToken cancellationToken)
    {
        var resposta = await handler.HandleAsync(new ObterFilialPorIdQuery(id), cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(resposta);
    }

    /// <summary>
    /// <c>PATCH /api/v1/filiais/{id}/celulas-ativas</c> (RF018). O <c>id</c> vem
    /// da rota; o body carrega apenas <c>celulasAtivas</c>. Como o command mescla
    /// id (rota) + request (body), o <see cref="ValidationFilter{T}"/> não é
    /// aplicável e validamos inline — mesmo padrão de <c>AlterarStatusUsuarioAsync</c>.
    /// </summary>
    private static async Task<Ok<FilialResponse>> AlterarCelulasAtivasAsync(
        Guid id,
        [FromBody] AlterarCelulasAtivasRequest? request,
        [FromServices] ICommandHandler<AlterarCelulasAtivasCommand, FilialResponse> handler,
        [FromServices] IValidator<AlterarCelulasAtivasCommand> validator,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw ValidationProblems.BodyAusente(
                MensagemPayloadInvalido,
                "Corpo da requisição ausente ou malformado.");
        }

        var command = new AlterarCelulasAtivasCommand(id, request.CelulasAtivas);

        var resultado = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        ValidationProblems.EnsureValid(resultado, MensagemPayloadInvalido);

        var resposta = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(resposta);
    }

    /// <summary>
    /// <c>PATCH /api/v1/filiais/{id}/status</c> (RF017). O <c>id</c> vem da
    /// rota; o body carrega apenas <c>ativo</c>. Validação inline — mesmo
    /// padrão de <see cref="AlterarCelulasAtivasAsync"/>.
    /// </summary>
    private static async Task<Ok<FilialResponse>> AlterarStatusAsync(
        Guid id,
        [FromBody] AlterarStatusFilialRequest? request,
        [FromServices] ICommandHandler<AlterarStatusFilialCommand, FilialResponse> handler,
        [FromServices] IValidator<AlterarStatusFilialCommand> validator,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw ValidationProblems.BodyAusente(
                MensagemPayloadInvalido,
                "Corpo da requisição ausente ou malformado.");
        }

        var command = new AlterarStatusFilialCommand(id, request.Ativo);

        var resultado = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        ValidationProblems.EnsureValid(resultado, MensagemPayloadInvalido);

        var resposta = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);
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

#pragma warning disable S2094, SA1502, CA1812 // Marcador para tipar ILogger por endpoint (categoria de log estável).

    /// <summary>Marker usado em <c>ILogger&lt;T&gt;</c> para categoria de log estável.</summary>
    internal sealed class CriarFilialEndpointMarker
    {
    }

#pragma warning restore S2094, SA1502, CA1812
}
