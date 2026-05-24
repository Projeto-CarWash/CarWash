using CarWash.Api.Filters;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Servicos.AlterarStatus;
using CarWash.Application.Servicos.Atualizar;
using CarWash.Application.Servicos.Common;
using CarWash.Application.Servicos.Criar;
using CarWash.Application.Servicos.Listar;
using CarWash.Application.Servicos.ObterPorId;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ValidationException = CarWash.Application.Common.Exceptions.ValidationException;

namespace CarWash.Api.Endpoints.Servicos;

/// <summary>
/// Endpoints de serviços (RF006). CRUD do catálogo de serviços com tipo,
/// preço e duração estimada.
/// </summary>
public static class ServicosEndpoints
{
    private const int TamanhoPaginaMaximo = 100;
    private const string MensagemPayloadInvalido = "Dados inválidos. Verifique os campos e tente novamente.";

    public static IEndpointRouteBuilder MapServicos(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var grupo = app.MapGroup("/api/v1/servicos")
            .WithTags("Serviços")
            .RequireAuthorization();

        grupo.MapPost("/", CriarAsync)
            .WithName("CriarServico")
            .Produces<CriarServicoResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict);

        grupo.MapGet("/", ListarAsync)
            .WithName("ListarServicos")
            .Produces<ListaServicosResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        grupo.MapGet("/{id:guid}", ObterPorIdAsync)
            .WithName("ObterServicoPorId")
            .Produces<ServicoResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        grupo.MapPatch("/{id:guid}", AtualizarAsync)
            .WithName("AtualizarServico")
            .Produces<ServicoResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        grupo.MapPatch("/{id:guid}/status", AlterarStatusAsync)
            .WithName("AlterarStatusServico")
            .Produces<ServicoResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<Created<CriarServicoResponse>> CriarAsync(
        [FromBody] CriarServicoRequest? request,
        [FromServices] ICommandHandler<CriarServicoCommand, CriarServicoResponse> handler,
        [FromServices] IValidator<CriarServicoCommand> validator,
        [FromServices] ILogger<CriarServicoEndpointMarker> logger,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw ValidationProblems.BodyAusente(
                "Dados do serviço inválidos. Verifique os campos e tente novamente.",
                "Corpo da requisição ausente ou malformado.");
        }

        var traceId = http.TraceIdentifier;
        var usuarioId = ObterUsuarioId(http);

        var command = new CriarServicoCommand(
            Nome: request.Nome,
            Preco: request.Preco,
            DuracaoMin: request.DuracaoMin,
            TraceId: traceId,
            UsuarioId: usuarioId);

        await ValidarAsync(validator, command, "Dados do serviço inválidos. Verifique os campos e tente novamente.", cancellationToken)
            .ConfigureAwait(false);

        var resposta = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Serviço cadastrado com sucesso. TraceId: {TraceId}. ServicoId: {ServicoId}. UsuarioId: {UsuarioId}",
            traceId, resposta.Id, usuarioId);

        return TypedResults.Created($"/api/v1/servicos/{resposta.Id}", resposta);
    }

    private static async Task<Ok<ListaServicosResponse>> ListarAsync(
        [FromQuery] string? busca,
        [FromQuery] bool? ativo,
        [FromServices] IQueryHandler<ListarServicosQuery, ListaServicosResponse> handler,
        HttpContext http,
        CancellationToken cancellationToken,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20)
    {
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
            new ListarServicosQuery(busca, ativo, pagina, tamanhoPagina),
            cancellationToken).ConfigureAwait(false);

        return TypedResults.Ok(resposta);
    }

    private static async Task<Ok<ServicoResponse>> ObterPorIdAsync(
        Guid id,
        [FromServices] IQueryHandler<ObterServicoPorIdQuery, ServicoResponse> handler,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var resposta = await handler.HandleAsync(new ObterServicoPorIdQuery(id), cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(resposta);
    }

    private static async Task<Ok<ServicoResponse>> AtualizarAsync(
        Guid id,
        [FromBody] AtualizarServicoRequest? request,
        [FromServices] ICommandHandler<AtualizarServicoCommand, ServicoResponse> handler,
        [FromServices] IValidator<AtualizarServicoCommand> validator,
        [FromServices] ILogger<AtualizarServicoEndpointMarker> logger,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw ValidationProblems.BodyAusente(
                "Dados do serviço inválidos. Verifique os campos e tente novamente.",
                "Corpo da requisição ausente ou malformado.");
        }

        var traceId = http.TraceIdentifier;
        var usuarioId = ObterUsuarioId(http);

        var command = new AtualizarServicoCommand(
            Id: id,
            Nome: request.Nome,
            Preco: request.Preco,
            DuracaoMin: request.DuracaoMin,
            TraceId: traceId,
            UsuarioId: usuarioId);

        await ValidarAsync(validator, command, "Dados do serviço inválidos. Verifique os campos e tente novamente.", cancellationToken)
            .ConfigureAwait(false);

        var resposta = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Serviço atualizado. TraceId: {TraceId}. ServicoId: {ServicoId}. UsuarioId: {UsuarioId}",
            traceId, id, usuarioId);

        return TypedResults.Ok(resposta);
    }

    private static async Task<Ok<ServicoResponse>> AlterarStatusAsync(
        Guid id,
        [FromBody] AlterarStatusServicoRequest? request,
        [FromServices] ICommandHandler<AlterarStatusServicoCommand, ServicoResponse> handler,
        [FromServices] IValidator<AlterarStatusServicoCommand> validator,
        [FromServices] ILogger<AlterarStatusServicoEndpointMarker> logger,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Ativo is null)
        {
            throw new ValidationException(
                MensagemPayloadInvalido,
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["ativo"] = ["Campo 'ativo' é obrigatório."],
                });
        }

        var traceId = http.TraceIdentifier;
        var usuarioId = ObterUsuarioId(http);

        var command = new AlterarStatusServicoCommand(id, request.Ativo, traceId, usuarioId);

        await ValidarAsync(validator, command, MensagemPayloadInvalido, cancellationToken).ConfigureAwait(false);

        var resposta = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Status do serviço alterado. TraceId: {TraceId}. ServicoId: {ServicoId}. Ativo: {Ativo}. UsuarioId: {UsuarioId}",
            traceId, id, request.Ativo.Value, usuarioId);

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
        var sub = http.User.FindFirst("sub")?.Value
            ?? http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

#pragma warning disable S2094, SA1502, CA1812 // Marcadores apenas para tipar o ILogger por endpoint (categoria de log estável).

    /// <summary>Marker usado em <c>ILogger&lt;T&gt;</c> para categoria de log estável.</summary>
    internal sealed class CriarServicoEndpointMarker
    {
    }

    /// <summary>Marker usado em <c>ILogger&lt;T&gt;</c> para categoria de log estável.</summary>
    internal sealed class AtualizarServicoEndpointMarker
    {
    }

    /// <summary>Marker usado em <c>ILogger&lt;T&gt;</c> para categoria de log estável.</summary>
    internal sealed class AlterarStatusServicoEndpointMarker
    {
    }

#pragma warning restore S2094, SA1502, CA1812
}
