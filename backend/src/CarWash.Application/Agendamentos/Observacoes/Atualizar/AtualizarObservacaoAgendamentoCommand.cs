using CarWash.Application.Abstractions.Messaging;

namespace CarWash.Application.Agendamentos.Observacoes.Atualizar;

public sealed record AtualizarObservacaoAgendamentoCommand(
    Guid AgendamentoId,
    Guid ObservacaoId,
    string? Texto,
    Guid UsuarioId,
    string TraceId) : ICommand<AtualizarObservacaoAgendamentoResponse>;
