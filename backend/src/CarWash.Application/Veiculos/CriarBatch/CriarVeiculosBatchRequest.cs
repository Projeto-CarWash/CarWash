namespace CarWash.Application.Veiculos.CriarBatch;

/// <summary>
/// DTO de entrada do <c>POST /api/v1/clientes/{clienteId}/veiculos/batch</c>.
/// Permite cadastrar múltiplos veículos em uma única requisição (RF005).
/// </summary>
public class CriarVeiculosBatchRequest
{
    public List<CriarVeiculoItemRequest> Veiculos { get; set; } = [];
}

/// <summary>
/// Item individual do batch de veículos.
/// </summary>
public class CriarVeiculoItemRequest
{
    public string? Placa { get; set; }

    public string? Modelo { get; set; }

    public string? Fabricante { get; set; }

    public string? Cor { get; set; }

    public int? Ano { get; set; }
}
