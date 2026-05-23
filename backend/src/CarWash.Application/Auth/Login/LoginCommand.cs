using CarWash.Application.Abstractions.Messaging;

namespace CarWash.Application.Auth.Login;

/// <summary>
/// Comando de login. O <c>Email</c> chega bruto — normalização (trim + lowercase)
/// acontece no handler para evitar oráculo de enumeração via 400 do validator.
/// Retorna <see cref="LoginResultado"/> (com refresh + access) — o endpoint extrai
/// o refresh para Set-Cookie e devolve apenas <c>LoginResponse</c> no body.
/// </summary>
public sealed record LoginCommand(string Email, string Senha) : ICommand<LoginResultado>;
