using System.Text.Json;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Agendamentos.Common;
using CarWash.Application.Agendamentos.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Domain.Common;
using CarWash.Domain.Entities;
using CarWash.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CarWash.Application.Agendamentos.Finalizar;

/// <summary>
/// Use case de finalização de atendimento (EM_ANDAMENTO → FINALIZADO) —
/// RF010/RF013. Valida o status atual (apenas EM_ANDAMENTO pode ser
/// finalizado; demais → 409), persiste com auditoria e histórico (evento
/// FINALIZADO — RN007). Agendamento finalizado libera a célula da filial
/// (RF008) e bloqueia edição/cancelamento (RF010).
/// </summary>
public sealed class FinalizarAgendamentoHandler
    : ICommandHandler<FinalizarAgendamentoCommand, FinalizarAgendamentoResponse>
{
    private readonly IAgendamentoRepository _agendamentos;
    private readonly ILogger<FinalizarAgendamentoHandler> _logger;

    public FinalizarAgendamentoHandler(
        IAgendamentoRepository agendamentos,
        ILogger<FinalizarAgendamentoHandler> logger)
    {
        _agendamentos = agendamentos;
        _logger = logger;
    }

    public async Task<FinalizarAgendamentoResponse> HandleAsync(
        FinalizarAgendamentoCommand command, CancellationToken cancellationToken)
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
            agendamento.Finalizar();
        }
        catch (DomainException ex)
        {
            // Transição inválida de estado → 409 (mesmo padrão do cancelamento).
            throw new TransicaoStatusException(ex.Message);
        }

        var historico = AgendamentoHistorico.Registrar(
            id: Guid.NewGuid(),
            agendamentoId: agendamento.Id,
            evento: EventoHistorico.Finalizado,
            usuarioId: usuarioId,
            payload: JsonSerializer.Serialize(new
            {
                statusAnterior = statusAnterior.ToDbValue(),
                statusNovo = agendamento.Status.ToDbValue(),
            }));

        await _agendamentos.SalvarAsync(
            agendamento,
            historico,
            command.TraceId,
            "AGENDAMENTO_FINALIZADO",
            usuarioId,
            JsonSerializer.Serialize(new
            {
                agendamento.Id,
                StatusNovo = agendamento.StatusRaw,
                agendamento.ValorTotal,
            }),
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Atendimento finalizado. AgendamentoId: {AgendamentoId}. UsuarioId: {UsuarioId}. TraceId: {TraceId}",
            agendamento.Id,
            usuarioId,
            command.TraceId);

        return new FinalizarAgendamentoResponse
        {
            Message = "Atendimento finalizado com sucesso.",
            Data = new FinalizarAgendamentoData
            {
                Id = agendamento.Id,
                Status = agendamento.Status.ToDbValue(),
                AtualizadoEm = agendamento.AtualizadoEm,
            },
            TraceId = command.TraceId,
        };
    }
}
