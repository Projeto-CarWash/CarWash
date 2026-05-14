namespace CarWash.Application.DTOs;

/// <summary>
/// Modelo de dados para a requisição de login.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Gets or sets o email do utilizador.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a senha do utilizador.
    /// </summary>
    public string Senha { get; set; } = string.Empty;
}
