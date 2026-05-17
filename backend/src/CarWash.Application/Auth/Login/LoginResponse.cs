using CarWash.Domain.Enums;

namespace CarWash.Application.Auth.Login;

/// <summary>
/// DTO de resposta HTTP do login bem-sucedido. <c>AccessToken</c> é um JWT
/// HMAC-SHA256 (RF001 + RT5). <c>ExpiresAt</c> é o vencimento do access em UTC.
///
/// <para>
/// O refresh token NUNCA aparece neste DTO — ele é entregue exclusivamente
/// via <c>Set-Cookie</c> httpOnly Secure SameSite=Strict.
/// </para>
/// </summary>
public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAt,
    LoginResponse.UsuarioLogado Usuario)
{
    /// <summary>Recorte público do usuário autenticado — nunca inclui hash de senha.</summary>
    public sealed record UsuarioLogado(Guid Id, string Nome, string Email, PerfilUsuario Perfil);

    public static LoginResponse From(LoginResultado resultado)
    {
        ArgumentNullException.ThrowIfNull(resultado);
        return new LoginResponse(
            resultado.AccessToken,
            resultado.AccessExpiresAt,
            new UsuarioLogado(
                resultado.Usuario.Id,
                resultado.Usuario.Nome,
                resultado.Usuario.Email,
                resultado.Usuario.Perfil));
    }
}
