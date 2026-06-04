namespace CarWash.Application.Veiculos.AtualizarParcial;

/// <summary>
/// DTO de entrada do <c>PATCH /api/v1/clientes/{clienteId}/veiculos/{veiculoId}</c>.
/// Atualização parcial — apenas campos enviados são alterados.
/// </summary>
public class AtualizarParcialVeiculoRequest
{
    public string? Placa { get; set; }

    public string? Modelo { get; set; }

    public string? Fabricante { get; set; }

    public string? Cor { get; set; }
}
