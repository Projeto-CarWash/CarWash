using System.Text.Json;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Agendamentos.Common;
using CarWash.Application.Agendamentos.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Domain.Common;
using CarWash.Domain.Entities;
using CarWash.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CarWash.Application.Agendamentos.Cancelar;

/// <summary>
/// Use case de cancelamento de agendamento (RF010). Valida o status atual
/// (AGENDADO→CANCELADO permitido; FINALIZADO/CANCELADO/EM_ANDAMENTO→409),
/// exige motivo e usuário, persiste com auditoria e histórico.
/// Concorrência otimista: se o status mudou entre o lookup e o commit,
/// o EF lança <c>DbUpdateConcurrencyException</c> → o handler traduz em 409.
/// </summary>
public sealed class CancelarAgendamentoHandler
    : ICommandHandler<CancelarAgendamentoCommand, CancelarAgendamentoResponse>
{
    private readonly IAgendamentoRepository _agendamentos;
    private readonly ILogger<CancelarAgendamentoHandler> _logger;

    public CancelarAgendamentoHandler(
        IAgendamentoRepository agendamentos,
        ILogger<CancelarAgendamentoHandler> logger)
    {
        _agendamentos = agendamentos;
        _logger = logger;
    }

    public async Task<CancelarAgendamentoResponse> HandleAsync(
        CancelarAgendamentoCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var usuarioId = command.UsuarioId ?? throw new ValidationException(
            "Não foi possível identificar o usuário autenticado.",
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["usuario"] = ["Usuário autenticado é obrigatório."],
            });

        var agendamento = await _agendamentos.ObterPorIdRastreadoAsync(
            command.AgendamentoId, cancellationToken).ConfigureAwait(false);

        if (agendamento is null)
        {
            throw new NotFoundException("Agendamento não encontrado.");
        }

        var statusAnterior = agendamento.Status;

        try
        {
            agendamento.Cancelar(command.MotivoCancelamento, usuarioId);
        }
        catch (DomainException ex) when (IsStatusTransitionBlock(ex))
        {
            _logger.LogWarning(
                ex,
                "Cancelamento rejeitado — status {StatusAtual} não permite cancelamento. AgendamentoId: {AgendamentoId}. UsuarioId: {UsuarioId}. TraceId: {TraceId}",
                statusAnterior.ToDbValue(),
                command.AgendamentoId,
                usuarioId,
                command.TraceId);

            throw MapDomainExceptionToConflict(ex, statusAnterior);
        }

        var historico = AgendamentoHistorico.Registrar(
            id: Guid.NewGuid(),
            agendamentoId: agendamento.Id,
            evento: EventoHistorico.Cancelado,
            usuarioId: usuarioId,
            payload: JsonSerializer.Serialize(new
            {
                statusAnterior = statusAnterior.ToDbValue(),
                statusNovo = StatusAgendamento.Cancelado.ToDbValue(),
                motivoCancelamento = command.MotivoCancelamento.Trim(),
                origem = command.Origem,
            }));

        await _agendamentos.SalvarAsync(
            agendamento, historico, command.TraceId, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Agendamento cancelado. AgendamentoId: {AgendamentoId}. StatusAnterior: {StatusAnterior}. " +
            "CanceladoPor: {CanceladoPor}. Motivo: {Motivo}. TraceId: {TraceId}",
            agendamento.Id,
            statusAnterior.ToDbValue(),
            usuarioId,
            command.MotivoCancelamento.Trim(),
            command.TraceId);

        return new CancelarAgendamentoResponse
        {
            Message = "Agendamento cancelado com sucesso.",
            Data = new CancelarAgendamentoData
            {
                Id = agendamento.Id,
                Status = agendamento.Status.ToDbValue(),
                CanceladoEm = agendamento.CanceladoEm,
                CanceladoPor = agendamento.CanceladoPor,
                MotivoCancelamento = agendamento.MotivoCancelamento,
            },
            TraceId = command.TraceId,
        };
    }

    private static bool IsStatusTransitionBlock(DomainException ex) =>
        ex.Message.Contains("não pode ser cancelado", StringComparison.OrdinalIgnoreCase);

    private static CancelamentoStatusException MapDomainExceptionToConflict(
        DomainException ex, StatusAgendamento statusAtual) => statusAtual switch
        {
            StatusAgendamento.Finalizado => new CancelamentoStatusException(
                CancelamentoStatusException.MensagemFinalizado),
            StatusAgendamento.Cancelado => new CancelamentoStatusException(
                CancelamentoStatusException.MensagemCancelado),
            StatusAgendamento.EmAndamento => new CancelamentoStatusException(
                CancelamentoStatusException.MensagemEmAndamento),
            _ => new CancelamentoStatusException(ex.Message),
        };
}
