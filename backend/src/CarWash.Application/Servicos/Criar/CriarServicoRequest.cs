namespace CarWash.Application.Servicos.Criar;

public sealed class CriarServicoRequest
{
    public string? Nome { get; set; }
    public decimal? Preco { get; set; }
    public int? DuracaoMin { get; set; }
}
