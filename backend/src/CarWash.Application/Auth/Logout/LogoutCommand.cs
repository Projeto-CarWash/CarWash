using CarWash.Application.Abstractions.Messaging;

namespace CarWash.Application.Auth.Logout;

/// <summary>
/// Comando de logout. <c>RefreshToken</c> é lido do cookie httpOnly pelo
/// endpoint. Idempotente — chamadas com cookie ausente/inválido sucedem em silêncio.
/// </summary>
public sealed record LogoutCommand(string? RefreshToken) : ICommand<LogoutResultado>;

/// <summary>Resultado vazio do logout — usado apenas para alinhar com o padrão CQRS.</summary>
public sealed record LogoutResultado();
