using CarWash.Application.Abstractions.Messaging;

namespace CarWash.Application.Agendamentos.Observacoes.Listar;

public sealed record ListarObservacoesAgendamentoQuery(
    Guid AgendamentoId,
    bool IncluirInativas,
    string TraceId) : IQuery<ListarObservacoesAgendamentoResponse>;
