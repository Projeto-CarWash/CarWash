using CarWash.Application.Abstractions.Messaging;

namespace CarWash.Application.Agendamentos.ObterPorId;

public sealed record ObterAgendamentoPorIdQuery(Guid Id) : IQuery<ObterAgendamentoPorIdResponse>;
