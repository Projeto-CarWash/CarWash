using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Usuarios.Common;
using CarWash.Domain.Enums;

namespace CarWash.Application.Usuarios.CriarUsuario;

/// <summary>
/// Comando de cadastro de usuário interno. <c>Senha</c> vem em texto puro do request
/// e é convertida em hash Argon2id pelo handler — NUNCA persistida em claro.
/// </summary>
public sealed record CriarUsuarioCommand(
    string Nome,
    string Email,
    string Senha,
    PerfilUsuario Perfil) : ICommand<UsuarioResponse>;
