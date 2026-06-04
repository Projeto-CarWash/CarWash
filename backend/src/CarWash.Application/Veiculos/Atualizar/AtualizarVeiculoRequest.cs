namespace CarWash.Application.Veiculos.Atualizar;

/// <summary>
/// DTO de entrada do <c>PUT /api/v1/clientes/{clienteId}/veiculos/{id}</c>.
/// Todos os campos são obrigatórios (substituição completa).
/// </summary>
public class AtualizarVeiculoRequest
{
    public string? Placa { get; set; }

    public string? Modelo { get; set; }

    public string? Fabricante { get; set; }

    public string? Cor { get; set; }

    public int? Ano { get; set; }
}
