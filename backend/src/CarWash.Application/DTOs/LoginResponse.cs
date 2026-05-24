namespace CarWash.Application.DTOs;

/// <summary>
/// Modelo de dados para a resposta de sucesso do login.
/// </summary>
public class LoginResponse : BaseResponse
{
    /// <summary>
    /// Gets or sets o token de acesso JWT.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets o token de atualização (Refresh Token).
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets os dados básicos do utilizador logado.
    /// </summary>
    public UsuarioResponse Usuario { get; set; } = null!;
}
