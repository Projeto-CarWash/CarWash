using CarWash.Application.Abstractions.Messaging;

namespace CarWash.Application.Agendamentos.Observacoes.Excluir;

public sealed record ExcluirObservacaoAgendamentoCommand(
    Guid AgendamentoId,
    Guid ObservacaoId,
    Guid UsuarioId,
    string TraceId) : ICommand<ExcluirObservacaoAgendamentoResponse>;
