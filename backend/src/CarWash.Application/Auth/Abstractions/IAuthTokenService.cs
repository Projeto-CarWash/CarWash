using CarWash.Domain.Entities;

namespace CarWash.Application.Auth.Abstractions;

/// <summary>
/// Emissor de access token de sessão para o login. No MVP é um token opaco
/// (Base64Url de 32 bytes aleatórios) com expiração explícita.
/// </summary>
public interface IAuthTokenService
{
    /// <summary>
    /// Emite um novo access token para o usuário autenticado. A implementação pode
    /// também persistir <c>UsuarioSessao</c> (refresh token hash) se desejado —
    /// no MVP retorna apenas o token opaco + <c>ExpiresAt</c> em UTC.
    /// </summary>
    Task<(string Token, DateTime ExpiresAt)> EmitirAsync(Usuario usuario, CancellationToken cancellationToken);
}
