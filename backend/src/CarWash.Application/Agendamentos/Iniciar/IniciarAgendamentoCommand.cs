using CarWash.Application.Abstractions.Messaging;

namespace CarWash.Application.Agendamentos.Iniciar;

/// <summary>
/// Comando de início de atendimento (transição AGENDADO → EM_ANDAMENTO).
/// Pré-requisito do fluxo de conclusão (RF010/RF013): apenas agendamentos
/// em andamento podem ser finalizados.
/// </summary>
public sealed record IniciarAgendamentoCommand(
    Guid AgendamentoId,
    string TraceId,
    Guid? UsuarioId) : ICommand<IniciarAgendamentoResponse>;
