using CarWash.Domain.Enums;

namespace CarWash.Application.Auth.Login;

/// <summary>
/// Resultado completo do <see cref="LoginHandler"/> — usado internamente pelo
/// endpoint para compor a resposta HTTP (<see cref="LoginResponse"/> no body)
/// e o cookie httpOnly de refresh.
///
/// <para>
/// <strong>Atenção:</strong> <c>RefreshToken</c> NUNCA vai no body. O endpoint
/// extrai esse campo apenas para o Set-Cookie httpOnly Secure SameSite=Strict.
/// </para>
/// </summary>
public sealed record LoginResultado(
    string AccessToken,
    DateTime AccessExpiresAt,
    string RefreshToken,
    DateTime RefreshExpiresAt,
    LoginResultado.UsuarioLogado Usuario)
{
    public sealed record UsuarioLogado(Guid Id, string Nome, string Email, PerfilUsuario Perfil, string Theme);
}
