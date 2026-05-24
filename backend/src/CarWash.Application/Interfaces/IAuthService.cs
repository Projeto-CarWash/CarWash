using CarWash.Application.DTOs;

namespace CarWash.Application.Interfaces;

/// <summary>
/// Interface que define os contratos para o serviço de autenticação.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Realiza o login do utilizador no sistema.
    /// </summary>
    /// <param name="request">Os dados de acesso.</param>
    /// <returns>A resposta contendo os tokens.</returns>
    Task<LoginResponse> LoginAsync(LoginRequest request);

    /// <summary>
    /// Atualiza o token de acesso de uma sessão.
    /// </summary>
    /// <param name="request">O token de atualização.</param>
    /// <returns>Os novos tokens gerados.</returns>
    Task<LoginResponse> RefreshAsync(TokenRequest request);

    /// <summary>
    /// Finaliza a sessão do utilizador.
    /// </summary>
    /// <param name="refreshToken">O token de atualização atual.</param>
    /// <returns>Uma tarefa assíncrona.</returns>
    Task LogoutAsync(string refreshToken);
}
