namespace CarWash.Application.Veiculos.AlterarStatus;

/// <summary>
/// DTO de entrada do <c>PATCH /api/v1/clientes/{clienteId}/veiculos/{id}/status</c>.
/// </summary>
public class AlterarStatusVeiculoRequest
{
    public bool? Ativo { get; set; }
}
