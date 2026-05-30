using CarWash.Application.Abstractions.Messaging;

namespace CarWash.Application.Agendamentos.Criar;

public sealed record CriarAgendamentoCommand(
    Guid FilialId,
    Guid ClienteId,
    Guid VeiculoId,
    DateTime Inicio,
    IReadOnlyList<Guid> ServicoIds,
    string? Observacoes,
    string TraceId,
    Guid? UsuarioId) : ICommand<CriarAgendamentoResponse>;
