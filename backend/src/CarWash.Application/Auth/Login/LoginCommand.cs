using CarWash.Application.Abstractions.Messaging;

namespace CarWash.Application.Auth.Login;

/// <summary>
/// Comando de login. O <c>Email</c> chega bruto — normalização (trim + lowercase)
/// acontece no handler para evitar oráculo de enumeração via 400 do validator.
/// </summary>
public sealed record LoginCommand(string Email, string Senha) : ICommand<LoginResponse>;
