namespace CarWash.Application.Veiculos.Criar;

/// <summary>
/// DTO de entrada do <c>POST /api/v1/clientes/{clienteId}/veiculos</c>. O
/// <c>ClienteId</c> vem da rota; <c>TraceId</c> e <c>UsuarioId</c> são preenchidos
/// pelo endpoint — não pertencem ao body.
/// </summary>
public class CriarVeiculoRequest
{
    public string? Placa { get; set; }

    public string? Modelo { get; set; }

    public string? Fabricante { get; set; }

    public string? Cor { get; set; }

    public int? Ano { get; set; }
}
