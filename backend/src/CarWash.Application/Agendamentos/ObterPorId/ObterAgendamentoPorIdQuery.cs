using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Agendamentos.Common;

namespace CarWash.Application.Agendamentos.ObterPorId;

/// <summary>
/// Consulta detalhada de agendamento por id (RF010).
/// </summary>
public sealed record ObterAgendamentoPorIdQuery(Guid Id, string TraceId) : IQuery<AgendamentoDetalhadoResponse>;
