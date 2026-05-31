namespace CarWash.Domain.Entities;

/// <summary>
/// Representa um utilizador no sistema.
/// </summary>
public class User
{
    /// <summary>
    /// Gets or sets o identificador único.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets o email do utilizador.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets o hash da senha.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets um valor indicando se o utilizador está ativo.
    /// </summary>
    public bool Active { get; set; } = true;

    /// <summary>
    /// Gets or sets o número de tentativas falhas de login.
    /// </summary>
    public int FailedLoginAttempts { get; set; }

    /// <summary>
    /// Gets or sets a data limite de bloqueio temporário.
    /// </summary>
    public DateTime? BlockedUntil { get; set; }

    /// <summary>
    /// Gets or sets a data de criação do registro.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
