using CarWash.Domain.Enums;

namespace CarWash.Application.Auth.Refresh;

/// <summary>
/// DTO de resposta HTTP do refresh bem-sucedido. Sem o refresh token (esse
/// vai pelo Set-Cookie httpOnly).
/// </summary>
public sealed record RefreshResponse(
    string AccessToken,
    DateTime ExpiresAt,
    RefreshResponse.UsuarioLogado Usuario)
{
    public sealed record UsuarioLogado(Guid Id, string Nome, string Email, PerfilUsuario Perfil);

    public static RefreshResponse From(RefreshResultado resultado)
    {
        ArgumentNullException.ThrowIfNull(resultado);
        return new RefreshResponse(
            resultado.AccessToken,
            resultado.AccessExpiresAt,
            new UsuarioLogado(
                resultado.Usuario.Id,
                resultado.Usuario.Nome,
                resultado.Usuario.Email,
                resultado.Usuario.Perfil));
    }
}
