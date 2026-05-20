namespace CarWash.Domain.Entities;

/// <summary>
/// Representa um veiculo vinculado a um cliente.
/// </summary>
public class Veiculo
{
    /// <summary>
    /// Gets or sets o identificador unico.
    /// </summary>
    public Guid Id { get; set; }

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
    /// Gets or sets o identificador do cliente vinculado.
    /// </summary>
    public Guid ClienteId { get; set; }

    /// <summary>
    /// Gets or sets o cliente vinculado.
    /// </summary>
    public Cliente Cliente { get; set; } = null!;

    /// <summary>
    /// Gets or sets a data de criacao do registro.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
