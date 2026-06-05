namespace CarWash.Application.DTOs;

/// <summary>
/// Modelo de dados para a requisicao de cadastro de veiculo.
/// </summary>
public class CriarVeiculoRequest
{
    /// <summary>
    /// Gets or sets a placa do veiculo.
    /// </summary>
    public string Placa { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets o modelo do veiculo.
    /// </summary>
    public string Modelo { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets o fabricante do veiculo.
    /// </summary>
    public string Fabricante { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a cor do veiculo.
    /// </summary>
    public string Cor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets o ano do veiculo.
    /// </summary>
    public int? Ano { get; set; }
}
