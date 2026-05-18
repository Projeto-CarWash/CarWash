using CarWash.Domain.Entities;

namespace CarWash.Application.Auth.Abstractions;

/// <summary>
/// Emissor de access tokens JWT assinados (HMAC-SHA256). Stateless: o backend
/// não persiste o access token. Validação é via <c>AddAuthentication(JwtBearer)</c>
/// com <c>TokenValidationParameters</c> (issuer/audience/lifetime/signing key).
/// O fluxo de refresh é responsabilidade do <see cref="IRefreshTokenService"/>.
/// </summary>
public interface IAccessTokenService
{
    /// <summary>
    /// Emite um novo access token JWT para o usuário autenticado.
    /// Claims emitidas: <c>sub</c>, <c>email</c>, <c>name</c>, <c>perfil</c>,
    /// <c>jti</c>, <c>iat</c>, <c>nbf</c>, <c>exp</c>, <c>iss</c>, <c>aud</c>.
    /// </summary>
    /// <returns>Token codificado + momento de expiração em UTC.</returns>
    (string Token, DateTime ExpiresAt) Emitir(Usuario usuario);
}
