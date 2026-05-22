using CarWash.Api.Filters;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Agendamentos.Common;
using CarWash.Application.Agendamentos.Criar;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace CarWash.Api.Endpoints.Agendamentos;

/// <summary>
/// Endpoints de agendamento (RF007/RF019/RF020/RF024). Defesa em camadas da RN011:
/// o conflito de veículo retorna 409 e o recurso inativo retorna 422.
/// </summary>
public static class AgendamentosEndpoints
{
    private const string MensagemPayloadInvalido =
        "Dados do agendamento inválidos. Verifique os campos e tente novamente.";

    public static IEndpointRouteBuilder MapAgendamentos(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var grupo = app.MapGroup("/api/v1/agendamentos")
            .WithTags("Agendamentos")
            .RequireAuthorization();

        grupo.MapPost("/", CriarAsync)
            .WithName("CriarAgendamento")
            .Produces<AgendamentoResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return app;
    }

    private static async Task<Created<AgendamentoResponse>> CriarAsync(
        [FromBody] CriarAgendamentoRequest? request,
        [FromServices] ICommandHandler<CriarAgendamentoCommand, AgendamentoResponse> handler,
        [FromServices] IValidator<CriarAgendamentoCommand> validator,
        [FromServices] ILogger<CriarAgendamentoEndpointMarker> logger,
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

        var command = new CriarAgendamentoCommand(
            FilialId: request.FilialId,
            ClienteId: request.ClienteId,
            VeiculoId: request.VeiculoId,
            ResponsavelId: request.ResponsavelId,
            Inicio: request.Inicio,
            ServicoIds: request.ServicoIds,
            Observacoes: request.Observacoes,
            TraceId: traceId,
            UsuarioId: usuarioId);

        var resultado = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        ValidationProblems.EnsureValid(resultado, MensagemPayloadInvalido);

        var resposta = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Agendamento criado com sucesso. TraceId: {TraceId}. AgendamentoId: {AgendamentoId}. "
            + "VeiculoId: {VeiculoId}. FilialId: {FilialId}. UsuarioId: {UsuarioId}",
            traceId,
            resposta.Id,
            resposta.VeiculoId,
            resposta.FilialId,
            usuarioId);

        return TypedResults.Created($"/api/v1/agendamentos/{resposta.Id}", resposta);
    }

    private static Guid? ObterUsuarioId(HttpContext http)
    {
        var sub = http.User.FindFirst("sub")?.Value
            ?? http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

#pragma warning disable S2094, SA1502, CA1812 // Marcador apenas para tipar o ILogger por endpoint (categoria de log estável).

    /// <summary>Marker usado em <c>ILogger&lt;T&gt;</c> para categoria de log estável.</summary>
    internal sealed class CriarAgendamentoEndpointMarker
    {
    }

#pragma warning restore S2094, SA1502, CA1812
}
