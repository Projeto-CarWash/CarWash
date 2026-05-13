using CarWash.Domain.Entities;
using CarWash.Domain.Enums;

namespace CarWash.Application.Usuarios.Common;

/// <summary>
/// DTO de saída de usuário. NÃO expõe <c>SenhaHash</c> (RNF003).
/// </summary>
public sealed record UsuarioResponse(
    Guid Id,
    string Nome,
    string Email,
    PerfilUsuario Perfil,
    bool Ativo,
    DateTime CriadoEm,
    DateTime AtualizadoEm)
{
    public static UsuarioResponse FromEntity(Usuario usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        return new UsuarioResponse(
            usuario.Id,
            usuario.Nome,
            usuario.EmailValor,
            usuario.Perfil,
            usuario.Ativo,
            usuario.CriadoEm,
            usuario.AtualizadoEm);
    }
}
