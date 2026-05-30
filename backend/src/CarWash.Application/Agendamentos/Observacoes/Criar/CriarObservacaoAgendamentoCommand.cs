using CarWash.Application.Abstractions.Messaging;

namespace CarWash.Application.Agendamentos.Observacoes.Criar;

public sealed record CriarObservacaoAgendamentoCommand(
    Guid AgendamentoId,
    string? Texto,
    Guid UsuarioId,
    string TraceId) : ICommand<CriarObservacaoAgendamentoResponse>;
