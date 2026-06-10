using CarWash.Application.Abstractions.Messaging;

namespace CarWash.Application.Agendamentos.Confirmar;

/// <summary>
/// Comando da etapa de confirmação do agendamento (RF015 — etapa 2). Persiste o
/// agendamento em transação única, idempotente por <c>IdempotencyKey</c>.
/// <c>TraceId</c> e <c>UsuarioId</c> são preenchidos pelo endpoint.
/// </summary>
public sealed record ConfirmarAgendamentoCommand(
    Guid FilialId,
    Guid ClienteId,
    Guid VeiculoId,
    Guid ResponsavelId,
    DateTime? Inicio,
    IReadOnlyList<Guid>? ServicoIds,
    string? Observacoes,
    bool? Confirmar,
    string? TokenConfirmacao,
    Guid? IdempotencyKey,
    string TraceId,
    Guid? UsuarioId) : ICommand<ConfirmarAgendamentoResultado>;
