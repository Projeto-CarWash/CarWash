using CarWash.Application.Abstractions.Messaging;

namespace CarWash.Application.Agendamentos.Cancelar;

/// <summary>
/// Comando de cancelamento de agendamento (RF010). O <c>motivoCancelamento</c>
/// é obrigatório e validado estruturalmente (trim, 5–500 chars).
/// <c>TraceId</c> e <c>UsuarioId</c> são preenchidos pelo endpoint.
/// </summary>
public sealed record CancelarAgendamentoCommand(
	Guid AgendamentoId,
	string MotivoCancelamento,
	string Origem,
	string TraceId,
	Guid? UsuarioId) : ICommand<CancelarAgendamentoResponse>;
