using CarWash.Api.Filters;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Agendamentos.Criar;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ValidationException = CarWash.Application.Common.Exceptions.ValidationException;

namespace CarWash.Api.Endpoints.Agendamentos;

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
            .Produces<CriarAgendamentoResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<Created<CriarAgendamentoResponse>> CriarAsync(
        [FromBody] CriarAgendamentoRequest? request,
        [FromServices] ICommandHandler<CriarAgendamentoCommand, CriarAgendamentoResponse> handler,
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
        FilialId: request.FilialId ?? Guid.Empty,
        ClienteId: request.ClienteId ?? Guid.Empty,
        VeiculoId: request.VeiculoId ?? Guid.Empty,
        ResponsavelId: request.ResponsavelId,
        Inicio: request.Inicio ?? default,
        ServicoIds: request.ServicoIds ?? [],
        Observacoes: request.Observacoes,
        TraceId: traceId,
        UsuarioId: usuarioId);

        await ValidarAsync(validator, command, MensagemPayloadInvalido, cancellationToken)
            .ConfigureAwait(false);

        var resposta = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Agendamento criado com sucesso. TraceId: {TraceId}. AgendamentoId: {AgendamentoId}. UsuarioId: {UsuarioId}",
            traceId, resposta.Data.Id, usuarioId);

        return TypedResults.Created($"/api/v1/agendamentos/{resposta.Data.Id}", resposta);
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

#pragma warning disable S2094, SA1502, CA1812
    internal sealed class CriarAgendamentoEndpointMarker
    {
    }
#pragma warning restore S2094, SA1502, CA1812
}
