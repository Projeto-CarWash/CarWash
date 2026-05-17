using CarWash.Application.Abstractions.Messaging;

namespace CarWash.Application.Auth.Refresh;

/// <summary>
/// Comando de renovação de access token. <c>RefreshToken</c> é o valor BRUTO
/// lido do cookie httpOnly pelo endpoint — nunca chega por body. Retorna
/// <see cref="RefreshResultado"/> com novo access + novo refresh (rotação:
/// a sessão anterior é revogada e uma nova é emitida).
/// </summary>
public sealed record RefreshCommand(string RefreshToken) : ICommand<RefreshResultado>;
