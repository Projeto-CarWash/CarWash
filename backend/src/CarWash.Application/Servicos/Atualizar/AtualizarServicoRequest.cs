namespace CarWash.Application.Servicos.Atualizar;

public sealed class AtualizarServicoRequest
{
    public string? Nome { get; set; }
    public decimal? Preco { get; set; }
    public int? DuracaoMin { get; set; }
}
