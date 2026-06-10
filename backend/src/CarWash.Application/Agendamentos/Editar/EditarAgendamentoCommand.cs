using CarWash.Application.Abstractions.Messaging;

namespace CarWash.Application.Agendamentos.Editar;

/// <summary>
/// Comando de edição de agendamento (RF010). A edição é permitida apenas
/// quando o status é <c>AGENDADO</c>. <c>TraceId</c> e <c>UsuarioId</c>
/// são preenchidos pelo endpoint.
/// </summary>
public sealed record EditarAgendamentoCommand(
    Guid AgendamentoId,
    DateTime? Inicio,
    DateTime? Fim,
    Guid? ResponsavelId,
    string? Observacoes,
    string TraceId,
    Guid? UsuarioId) : ICommand<EditarAgendamentoResponse>;
