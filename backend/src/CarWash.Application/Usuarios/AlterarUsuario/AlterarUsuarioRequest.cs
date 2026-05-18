using CarWash.Domain.Enums;

namespace CarWash.Application.Usuarios.AlterarUsuario;

/// <summary>
/// DTO de entrada do <c>PUT /api/v1/usuarios/{id}</c>. O <c>id</c> vem da rota;
/// o body carrega nome, e-mail e perfil. A composição do
/// <see cref="AlterarUsuarioCommand"/> acontece no endpoint.
/// </summary>
public sealed record AlterarUsuarioRequest(string Nome, string Email, PerfilUsuario Perfil);
