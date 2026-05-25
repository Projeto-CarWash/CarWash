namespace CarWash.Application.Veiculos.Common;

/// <summary>
/// DTO de resposta de leitura de veículo. Espelha o agregado <c>Veiculo</c>
/// após persistência (RF005 / RF022).
/// </summary>
public class VeiculoResponse
{
    public Guid Id { get; set; }

    public Guid ClienteId { get; set; }

    public string Placa { get; set; } = string.Empty;

    public string Modelo { get; set; } = string.Empty;

    public string Fabricante { get; set; } = string.Empty;

    public string Cor { get; set; } = string.Empty;

    public int? Ano { get; set; }

    public bool Ativo { get; set; }

    public DateTime CriadoEm { get; set; }

    public DateTime AtualizadoEm { get; set; }
}
