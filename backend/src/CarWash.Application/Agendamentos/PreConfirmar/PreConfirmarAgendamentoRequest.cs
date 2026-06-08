namespace CarWash.Application.Agendamentos.PreConfirmar;

/// <summary>
/// DTO de entrada do <c>POST /api/v1/agendamentos/pre-confirmacao</c> (RF015).
/// Mesmos campos de negócio da criação direta — nada é persistido nesta etapa.
/// <c>TraceId</c> e <c>UsuarioId</c> são preenchidos pelo endpoint.
/// </summary>
public sealed class PreConfirmarAgendamentoRequest
{
    public Guid FilialId { get; set; }

    public Guid ClienteId { get; set; }

    public Guid VeiculoId { get; set; }

    public Guid ResponsavelId { get; set; }

    public DateTime? Inicio { get; set; }

    public IReadOnlyList<Guid>? ServicoIds { get; set; }

    public string? Observacoes { get; set; }
}
