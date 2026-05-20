namespace CarWash.Domain.Entities;

/// <summary>
/// Representa um cliente no sistema.
/// </summary>
public class Cliente
{
    /// <summary>
    /// Gets or sets o identificador unico.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets o nome do cliente.
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a data de criacao do registro.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets os veiculos vinculados ao cliente.
    /// </summary>
    public ICollection<Veiculo> Veiculos { get; set; } = new List<Veiculo>();
}
