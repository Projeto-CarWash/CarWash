using CarWash.Application.Abstractions.Messaging;

namespace CarWash.Application.Agendamentos.Finalizar;

/// <summary>
/// Comando de finalização de atendimento (EM_ANDAMENTO → FINALIZADO) —
/// RF010/RF013. Agendamento finalizado libera a célula da filial (RF008),
/// deixa de bloquear a janela do veículo (RN011) e passa a compor o
/// faturamento do dashboard (RF013).
/// </summary>
public sealed record FinalizarAgendamentoCommand(
    Guid AgendamentoId,
    string TraceId,
    Guid? UsuarioId) : ICommand<FinalizarAgendamentoResponse>;
