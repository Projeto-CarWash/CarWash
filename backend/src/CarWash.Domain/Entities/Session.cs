namespace CarWash.Domain.Entities;

/// <summary>
/// Representa uma sessão ativa de um utilizador.
/// </summary>
public class Session
{
    /// <summary>
    /// Gets or sets o identificador único da sessão.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets o identificador do utilizador atrelado.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets a referência da entidade de utilizador.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets o hash do token de renovação.
    /// </summary>
    public string RefreshTokenHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a data e hora de expiração.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets um valor indicando se a sessão foi revogada manualmente.
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// Gets or sets a data e hora de criação.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
