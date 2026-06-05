using CarWash.Api.Filters;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Responsaveis.Criar;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

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

        return app;
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

        var traceId = http.TraceIdentifier;
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
            traceId, resposta.Data.ResponsavelId, clienteTitularId, usuarioId);

        return TypedResults.Created(
            $"/api/v1/clientes/{clienteTitularId}/responsaveis/{resposta.Data.ResponsavelId}",
            resposta);
    }

    private static Guid? ObterUsuarioId(HttpContext http)
    {
        var sub = http.User.FindFirst("sub")?.Value
            ?? http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

#pragma warning disable S2094, SA1502, CA1812 // Marker para tipar o ILogger por endpoint (categoria estável).
    internal sealed class CriarResponsavelEndpointMarker
    {
    }
#pragma warning restore S2094, SA1502, CA1812
}
