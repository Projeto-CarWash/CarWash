using CarWash.Domain.Enums;

namespace CarWash.Application.Auth.Refresh;

/// <summary>
/// Resultado da renovação. O endpoint extrai <c>RefreshToken</c> para
/// Set-Cookie httpOnly e devolve apenas <c>AccessToken</c> + dados do usuário no body.
/// </summary>
public sealed record RefreshResultado(
    string AccessToken,
    DateTime AccessExpiresAt,
    string RefreshToken,
    DateTime RefreshExpiresAt,
    RefreshResultado.UsuarioLogado Usuario)
{
    public sealed record UsuarioLogado(Guid Id, string Nome, string Email, PerfilUsuario Perfil);
}
