using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Usuarios.Common;
using CarWash.Domain.Enums;

namespace CarWash.Application.Usuarios.CriarUsuario;

/// <summary>
/// Comando de cadastro de usuário interno. <c>Senha</c> vem em texto puro do request
/// e é convertida em hash Argon2id pelo handler — NUNCA persistida em claro.
/// <para>
/// <c>Perfil</c> é <c>nullable</c> no contrato de entrada para que a ausência no body
/// seja capturada explicitamente pelo validator (BUG-U003 — antes o default <c>0</c>
/// caía silenciosamente em <c>Admin</c>). O handler propaga <c>.Value</c> após a
/// validação garantir presença.
/// </para>
/// </summary>
public sealed record CriarUsuarioCommand(
    string Nome,
    string Email,
    string Senha,
    PerfilUsuario? Perfil) : ICommand<UsuarioResponse>;
