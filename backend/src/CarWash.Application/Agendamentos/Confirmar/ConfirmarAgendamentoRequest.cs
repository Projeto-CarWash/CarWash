namespace CarWash.Application.Agendamentos.Confirmar;

/// <summary>
/// DTO de entrada do <c>POST /api/v1/agendamentos/confirmar</c> (RF015 — etapa 2).
/// Reenvia os campos de negócio do agendamento mais a confirmação explícita, o
/// <c>tokenConfirmacao</c> emitido na prévia e a <c>idempotencyKey</c>.
/// <c>TraceId</c> e <c>UsuarioId</c> são preenchidos pelo endpoint.
/// </summary>
public sealed class ConfirmarAgendamentoRequest
{
    public Guid FilialId { get; set; }

    public Guid ClienteId { get; set; }

    public Guid VeiculoId { get; set; }

    public Guid? ResponsavelId { get; set; }

    public DateTime? Inicio { get; set; }

    public IReadOnlyList<Guid>? ServicoIds { get; set; }

    public string? Observacoes { get; set; }

    /// <summary>Confirmação explícita do usuário — deve ser <c>true</c> para concluir.</summary>
    public bool? Confirmar { get; set; }

    /// <summary>Token assinado recebido na pré-confirmação.</summary>
    public string? TokenConfirmacao { get; set; }

    /// <summary>Chave de idempotência (GUID) — duplo clique/retry produzem um só agendamento.</summary>
    public Guid? IdempotencyKey { get; set; }
}
