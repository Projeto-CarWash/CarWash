namespace CarWash.Application.Agendamentos.Observacoes.Common;

public sealed class AgendamentoObservacaoResponse
{
    public Guid Id { get; set; }

    public Guid AgendamentoId { get; set; }

    public string Texto { get; set; } = string.Empty;

    public bool Ativo { get; set; }

    public DateTimeOffset CriadoEm { get; set; }

    public Guid CriadoPor { get; set; }

    public DateTimeOffset? AtualizadoEm { get; set; }

    public Guid? AtualizadoPor { get; set; }

    public DateTimeOffset? ExcluidoEm { get; set; }

    public Guid? ExcluidoPor { get; set; }
}
