namespace CarWash.Application.DTOs;

/// <summary>
/// Modelo de dados para a requisição de renovação de token.
/// </summary>
public class TokenRequest
{
    /// <summary>
    /// Gets or sets o token de atualização (Refresh Token).
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}
