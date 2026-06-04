namespace CarWash.Application.Agendamentos.Criar;

public class CriarAgendamentoRequest
{
    public Guid? FilialId { get; set; }

    public Guid? ClienteId { get; set; }

    public Guid? VeiculoId { get; set; }

    public Guid? ResponsavelId { get; set; }

    public DateTime? Inicio { get; set; }

    public List<Guid>? ServicoIds { get; set; }

    public string? Observacoes { get; set; }
}
