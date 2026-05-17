using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Usuarios.Common;
using CarWash.Domain.Enums;

namespace CarWash.Application.Usuarios.AlterarUsuario;

/// <summary>
/// Comando para atualização dos dados cadastrais de um usuário interno (RF014).
/// Não altera senha nem status — usar comandos dedicados.
/// </summary>
public sealed record AlterarUsuarioCommand(
    Guid Id,
    string Nome,
    string Email,
    PerfilUsuario Perfil) : ICommand<UsuarioResponse>;

public sealed record AlterarUsuarioRequest(string Nome, string Email, PerfilUsuario Perfil);
