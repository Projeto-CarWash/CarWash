namespace CarWash.Application.Servicos.Criar;

/// <summary>
/// DTO de entrada do <c>POST /api/v1/servicos</c>. O <c>TraceId</c> e o
/// <c>UsuarioId</c> são preenchidos pelo endpoint — não pertencem ao body.
/// </summary>
public class CriarServicoRequest
{
    public string? Nome { get; set; }

    public decimal? Preco { get; set; }

    public int? DuracaoMin { get; set; }
}
