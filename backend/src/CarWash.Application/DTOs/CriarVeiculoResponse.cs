namespace CarWash.Application.DTOs;

/// <summary>
/// Modelo de dados para a resposta de operacoes com veiculo.
/// </summary>
public class VeiculoResponse : BaseResponse
{
    /// <summary>
    /// Gets or sets o identificador do veiculo.
    /// </summary>
    public Guid Id { get; set; }
}
