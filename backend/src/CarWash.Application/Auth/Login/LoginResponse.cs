using CarWash.Domain.Enums;

namespace CarWash.Application.Auth.Login;

/// <summary>
/// Resposta do login bem-sucedido. <c>AccessToken</c> é um token opaco
/// Base64Url (MVP); <c>ExpiresAt</c> em UTC.
/// </summary>
public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAt,
    LoginResponse.UsuarioLogado Usuario)
{
    /// <summary>Recorte público do usuário autenticado — nunca inclui hash de senha.</summary>
    public sealed record UsuarioLogado(Guid Id, string Nome, string Email, PerfilUsuario Perfil);
}
