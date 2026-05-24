namespace CarWash.Application.Servicos.Atualizar;

/// <summary>
/// DTO de entrada do <c>PUT /api/v1/servicos/{id}</c>.
/// </summary>
public class AtualizarServicoRequest
{
    public string? Nome { get; set; }

    public decimal? Preco { get; set; }

    public int? DuracaoMin { get; set; }
}
