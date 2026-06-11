using CarWash.Api.Filters;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Responsaveis.AlterarStatus;
using CarWash.Application.Responsaveis.Atualizar;
using CarWash.Application.Responsaveis.Common;
using CarWash.Application.Responsaveis.Criar;
using CarWash.Application.Responsaveis.Listar;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ValidationException = CarWash.Application.Common.Exceptions.ValidationException;

namespace CarWash.Api.Endpoints.Responsaveis;

public static class ResponsaveisEndpoints
{
    private const string MensagemPayloadInvalido =
        "Dados do responsável inválidos. Verifique os campos e tente novamente.";

    public static IEndpointRouteBuilder MapResponsaveis(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var grupo = app.MapGroup("/api/v1/clientes/{clienteTitularId:guid}/responsaveis")
            .WithTags("Responsaveis")
            .RequireAuthorization();

        grupo.MapPost("/", CriarAsync)
            .WithName("CriarResponsavel")
            .Produces<CriarResponsavelResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        grupo.MapGet("/", ListarAsync)
            .WithName("ListarResponsaveis")
            .Produces<ListaResponsaveisResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        grupo.MapPut("/{id:guid}", AtualizarAsync)
            .WithName("AtualizarResponsavel")
            .Produces<ResponsavelResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        grupo.MapPatch("/{id:guid}/status", AlterarStatusAsync)
            .WithName("AlterarStatusResponsavel")
            .Produces<ResponsavelResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<Ok<IReadOnlyList<ResponsavelListaItem>>> ListarAsync(
        Guid clienteTitularId,
        [FromServices] IQueryHandler<ListarResponsaveisPorClienteQuery, IReadOnlyList<ResponsavelListaItem>> handler,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        string traceId = http.TraceIdentifier;

        var query = new ListarResponsaveisPorClienteQuery(clienteTitularId, traceId);

        var resultado = await handler.HandleAsync(query, cancellationToken).ConfigureAwait(false);

        return TypedResults.Ok(resultado);
    }

    private static async Task<Created<CriarResponsavelResponse>> CriarAsync(
        Guid clienteTitularId,
        [FromBody] CriarResponsavelRequest? request,
        [FromServices] ICommandHandler<CriarResponsavelCommand, CriarResponsavelResponse> handler,
        [FromServices] IValidator<CriarResponsavelCommand> validator,
        [FromServices] ILogger<CriarResponsavelEndpointMarker> logger,
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

        var command = new CriarResponsavelCommand(
            ClienteTitularId: clienteTitularId,
            Nome: request.Nome,
            Documento: request.Documento,
            Telefone: request.Telefone,
            Email: request.Email,
            GrauVinculo: request.GrauVinculo,
            TraceId: traceId,
            UsuarioId: usuarioId);

        var resultado = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        ValidationProblems.EnsureValid(resultado, MensagemPayloadInvalido);

        var resposta = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Responsável cadastrado com sucesso. TraceId: {TraceId}. ResponsavelId: {ResponsavelId}. ClienteTitularId: {ClienteTitularId}. UsuarioId: {UsuarioId}",
            traceId,
            resposta.Data.ResponsavelId,
            clienteTitularId,
            usuarioId);

        return TypedResults.Created(
            $"/api/v1/clientes/{clienteTitularId}/responsaveis/{resposta.Data.ResponsavelId}",
            resposta);
    }

    private static async Task<Ok<ListaResponsaveisResponse>> ListarAsync(
        Guid clienteTitularId,
        [FromServices] IQueryHandler<ListarResponsaveisQuery, ListaResponsaveisResponse> handler,
        CancellationToken cancellationToken)
    {
        var resposta = await handler.HandleAsync(
            new ListarResponsaveisQuery(clienteTitularId),
            cancellationToken).ConfigureAwait(false);

        return TypedResults.Ok(resposta);
    }

    private static async Task<Ok<ResponsavelResponse>> AtualizarAsync(
        Guid clienteTitularId,
        Guid id,
        [FromBody] AtualizarResponsavelRequest? request,
        [FromServices] ICommandHandler<AtualizarResponsavelCommand, ResponsavelResponse> handler,
        [FromServices] IValidator<AtualizarResponsavelCommand> validator,
        [FromServices] ILogger<AtualizarResponsavelEndpointMarker> logger,
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

        var command = new AtualizarResponsavelCommand(
            ResponsavelId: id,
            ClienteTitularId: clienteTitularId,
            Nome: request.Nome,
            Telefone: request.Telefone,
            Email: request.Email,
            GrauVinculo: request.GrauVinculo,
            CamposExtras: request.CamposExtras,
            TraceId: traceId,
            UsuarioId: usuarioId);

        var resultado = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        ValidationProblems.EnsureValid(resultado, MensagemPayloadInvalido);

        var resposta = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Responsável atualizado. ResponsavelId: {ResponsavelId}. ClienteTitularId: {ClienteTitularId}. UsuarioId: {UsuarioId}",
            id,
            clienteTitularId,
            usuarioId);

        return TypedResults.Ok(resposta);
    }

    private static async Task<Ok<ResponsavelResponse>> AlterarStatusAsync(
        Guid clienteTitularId,
        Guid id,
        [FromBody] AlterarStatusResponsavelRequest? request,
        [FromServices] ICommandHandler<AlterarStatusResponsavelCommand, ResponsavelResponse> handler,
        [FromServices] IValidator<AlterarStatusResponsavelCommand> validator,
        [FromServices] ILogger<AlterarStatusResponsavelEndpointMarker> logger,
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

        string traceId = http.TraceIdentifier;
        var usuarioId = ObterUsuarioId(http);

        var command = new AlterarStatusResponsavelCommand(
            ResponsavelId: id,
            ClienteTitularId: clienteTitularId,
            Ativo: request.Ativo,
            TraceId: traceId,
            UsuarioId: usuarioId);

        var resultado = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        ValidationProblems.EnsureValid(resultado, MensagemPayloadInvalido);

        var resposta = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Status do responsável alterado. ResponsavelId: {ResponsavelId}. Ativo: {Ativo}. ClienteTitularId: {ClienteTitularId}. UsuarioId: {UsuarioId}",
            id,
            request.Ativo.Value,
            clienteTitularId,
            usuarioId);

        return TypedResults.Ok(resposta);
    }

    private static Guid? ObterUsuarioId(HttpContext http)
    {
        string? sub = http.User.FindFirst("sub")?.Value
            ?? http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    #pragma warning disable S2094, SA1502, CA1812 // Markers para tipar o ILogger por endpoint (categoria estável).
    internal sealed class CriarResponsavelEndpointMarker
    {
    }

    internal sealed class AtualizarResponsavelEndpointMarker
    {
    }

    internal sealed class AlterarStatusResponsavelEndpointMarker
    {
    }
    #pragma warning restore S2094, SA1502, CA1812
}
