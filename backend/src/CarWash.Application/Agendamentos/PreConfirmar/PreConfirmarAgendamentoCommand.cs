using CarWash.Application.Abstractions.Messaging;

namespace CarWash.Application.Agendamentos.PreConfirmar;

/// <summary>
/// Comando da etapa de pré-confirmação do agendamento (RF015). Não persiste nada;
/// calcula o resumo de revisão e emite o <c>tokenConfirmacao</c>. <c>TraceId</c> e
/// <c>UsuarioId</c> são preenchidos pelo endpoint a partir do <c>HttpContext</c>.
/// </summary>
public sealed record PreConfirmarAgendamentoCommand(
    Guid FilialId,
    Guid ClienteId,
    Guid VeiculoId,
    Guid ResponsavelId,
    DateTime? Inicio,
    IReadOnlyList<Guid>? ServicoIds,
    string? Observacoes,
    string TraceId,
    Guid? UsuarioId) : ICommand<PreConfirmacaoResponse>;
