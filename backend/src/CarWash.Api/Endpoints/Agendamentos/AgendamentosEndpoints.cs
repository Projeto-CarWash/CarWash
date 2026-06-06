using CarWash.Api.Filters;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Agendamentos.Cancelar;
using CarWash.Application.Agendamentos.Common;
using CarWash.Application.Agendamentos.Confirmar;
using CarWash.Application.Agendamentos.Criar;
using CarWash.Application.Agendamentos.PreConfirmar;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace CarWash.Api.Endpoints.Agendamentos;

/// <summary>
/// Endpoints de agendamento (RF007/RF015/RF019/RF020/RF024). Defesa em camadas da
/// RN011: o conflito de veículo retorna 409 e o recurso inativo retorna 422.
/// O RF015 adiciona o fluxo de duas etapas: <c>/pre-confirmacao</c> (revisão, sem
/// persistência) e <c>/confirmar</c> (persistência idempotente).
/// </summary>
public static class AgendamentosEndpoints
{
    private const string MensagemPayloadInvalido =
        "Dados do agendamento inválidos. Verifique os campos e tente novamente.";

    /// <summary>Header de resposta que sinaliza um replay idempotente (RF015).</summary>
    private const string HeaderIdempotentReplay = "Idempotent-Replay";

    public static IEndpointRouteBuilder MapAgendamentos(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var grupo = app.MapGroup("/api/v1/agendamentos")
            .WithTags("Agendamentos")
            .RequireAuthorization();

        // RF007 — criação direta. Mantida e marcada Deprecated a partir do RF015
        // (ADR 0004): o frontend passa a usar /pre-confirmacao + /confirmar.
        grupo.MapPost("/", CriarAsync)
            .WithName("CriarAgendamento")
            .WithMetadata(new ObsoleteAttribute(
                "RF015/ADR 0004: use /pre-confirmacao + /confirmar. Mantido para integrações e testes."))
            .Produces<AgendamentoResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        // RF015 — etapa 1: pré-confirmação (revisão das informações, sem persistir).
        grupo.MapPost("/pre-confirmacao", PreConfirmarAsync)
            .WithName("PreConfirmarAgendamento")
            .Produces<PreConfirmacaoResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        // RF015 — etapa 2: confirmação idempotente (persiste em transação única).
        grupo.MapPost("/confirmar", ConfirmarAsync)
            .WithName("ConfirmarAgendamento")
            .Produces<AgendamentoResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status410Gone)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        // RF010 — cancelamento de agendamento com motivo obrigatório.
        grupo.MapPatch("/{id:guid}/cancelar", CancelarAsync)
            .WithName("CancelarAgendamento")
            .Produces<CancelarAgendamentoResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

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

        string traceId = http.TraceIdentifier;
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

    private static async Task<Ok<PreConfirmacaoResponse>> PreConfirmarAsync(
        [FromBody] PreConfirmarAgendamentoRequest? request,
        [FromServices] ICommandHandler<PreConfirmarAgendamentoCommand, PreConfirmacaoResponse> handler,
        [FromServices] IValidator<PreConfirmarAgendamentoCommand> validator,
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

        string traceId = http.TraceIdentifier;
        var usuarioId = ObterUsuarioId(http);

        var command = new PreConfirmarAgendamentoCommand(
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
            "Pré-confirmação de agendamento gerada. TraceId: {TraceId}. FilialId: {FilialId}. "
            + "VeiculoId: {VeiculoId}. UsuarioId: {UsuarioId}. ExpiraEm: {ExpiraEm:o}",
            traceId,
            resposta.Resumo.Filial.Id,
            resposta.Resumo.Veiculo.Id,
            usuarioId,
            resposta.ExpiraEm);

        return TypedResults.Ok(resposta);
    }

    private static async Task<Created<AgendamentoResponse>> ConfirmarAsync(
        [FromBody] ConfirmarAgendamentoRequest? request,
        [FromServices] ICommandHandler<ConfirmarAgendamentoCommand, ConfirmarAgendamentoResultado> handler,
        [FromServices] IValidator<ConfirmarAgendamentoCommand> validator,
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

        string traceId = http.TraceIdentifier;
        var usuarioId = ObterUsuarioId(http);

        var command = new ConfirmarAgendamentoCommand(
            FilialId: request.FilialId,
            ClienteId: request.ClienteId,
            VeiculoId: request.VeiculoId,
            ResponsavelId: request.ResponsavelId,
            Inicio: request.Inicio,
            ServicoIds: request.ServicoIds,
            Observacoes: request.Observacoes,
            Confirmar: request.Confirmar,
            TokenConfirmacao: request.TokenConfirmacao,
            IdempotencyKey: request.IdempotencyKey,
            TraceId: traceId,
            UsuarioId: usuarioId);

        var resultado = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        ValidationProblems.EnsureValid(resultado, MensagemPayloadInvalido);

        var confirmacao = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);
        var resposta = confirmacao.Agendamento;

        if (confirmacao.EhReplay)
        {
            http.Response.Headers[HeaderIdempotentReplay] = "true";
            logger.LogInformation(
                "Confirmação de agendamento devolvida via replay idempotente. TraceId: {TraceId}. "
                + "AgendamentoId: {AgendamentoId}. UsuarioId: {UsuarioId}",
                traceId,
                resposta.Id,
                usuarioId);
        }
        else
        {
            logger.LogInformation(
                "Agendamento confirmado com sucesso. TraceId: {TraceId}. AgendamentoId: {AgendamentoId}. "
                + "VeiculoId: {VeiculoId}. FilialId: {FilialId}. UsuarioId: {UsuarioId}",
                traceId,
                resposta.Id,
                resposta.VeiculoId,
                resposta.FilialId,
                usuarioId);
        }

        return TypedResults.Created($"/api/v1/agendamentos/{resposta.Id}", resposta);
    }

    private static async Task<Ok<CancelarAgendamentoResponse>> CancelarAsync(
        Guid id,
        [FromBody] CancelarAgendamentoRequest? request,
        [FromServices] ICommandHandler<CancelarAgendamentoCommand, CancelarAgendamentoResponse> handler,
        [FromServices] IValidator<CancelarAgendamentoCommand> validator,
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

        string traceId = http.TraceIdentifier;
        var usuarioId = ObterUsuarioId(http);

        var command = new CancelarAgendamentoCommand(
            AgendamentoId: id,
            MotivoCancelamento: request.MotivoCancelamento ?? string.Empty,
            Origem: request.Origem ?? string.Empty,
            TraceId: traceId,
            UsuarioId: usuarioId);

        var resultado = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        ValidationProblems.EnsureValid(resultado, MensagemPayloadInvalido);

        var resposta = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Agendamento cancelado via endpoint. TraceId: {TraceId}. AgendamentoId: {AgendamentoId}. UsuarioId: {UsuarioId}",
            traceId,
            id,
            usuarioId);

        return TypedResults.Ok(resposta);
    }

    private static Guid? ObterUsuarioId(HttpContext http)
    {
        string? sub = http.User.FindFirst("sub")?.Value
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
