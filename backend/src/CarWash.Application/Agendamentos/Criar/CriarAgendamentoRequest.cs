namespace CarWash.Application.Agendamentos.Criar;

/// <summary>
/// DTO de entrada do <c>POST /api/v1/agendamentos</c>. O <c>TraceId</c> e o
/// <c>UsuarioId</c> são preenchidos pelo endpoint — não pertencem ao body.
/// </summary>
public sealed class CriarAgendamentoRequest
{
    public Guid FilialId { get; set; }

    public Guid ClienteId { get; set; }

    public Guid VeiculoId { get; set; }

    public Guid ResponsavelId { get; set; }

    public DateTime? Inicio { get; set; }

    public IReadOnlyList<Guid>? ServicoIds { get; set; }

    public string? Observacoes { get; set; }
}
