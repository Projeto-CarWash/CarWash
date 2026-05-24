namespace CarWash.Application.DTOs;

/// <summary>
/// Modelo de dados para a resposta com informações do utilizador.
/// </summary>
public class UsuarioResponse
{
    /// <summary>
    /// Gets or sets o identificador único do utilizador.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets o email do utilizador.
    /// </summary>
    public string Email { get; set; } = string.Empty;
}
