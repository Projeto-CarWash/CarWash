using CarWash.Application.Abstractions;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Auth.Abstractions;
using Microsoft.Extensions.Logging;

namespace CarWash.Application.Auth.Logout;

/// <summary>
/// Use case de logout. Revoga a sessão correspondente ao refresh token recebido.
/// Idempotente — chamadas sem token / token inválido completam normalmente
/// (o cliente sempre apaga o cookie). Audita apenas quando há sessão real revogada.
/// </summary>
public sealed class LogoutHandler : ICommandHandler<LogoutCommand, LogoutResultado>
{
    public const string EventoSucesso = "UsuarioLogout";
    public const string EntidadeAuditoria = "UsuarioSessao";

    private readonly IRefreshTokenService _refreshTokens;
    private readonly IAuditLogger _auditoria;
    private readonly ICurrentRequestContext _contexto;
    private readonly ILogger<LogoutHandler> _log;

    public LogoutHandler(
        IRefreshTokenService refreshTokens,
        IAuditLogger auditoria,
        ICurrentRequestContext contexto,
        ILogger<LogoutHandler> log)
    {
        _refreshTokens = refreshTokens;
        _auditoria = auditoria;
        _contexto = contexto;
        _log = log;
    }

    public async Task<LogoutResultado> HandleAsync(LogoutCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            // Sem cookie / cookie vazio: response 200 mesmo assim — cliente apaga o cookie local.
            return new LogoutResultado();
        }

        await _refreshTokens.RevogarAsync(command.RefreshToken, cancellationToken).ConfigureAwait(false);

        _contexto.DefinirEvento(EventoSucesso);
        await _auditoria.LogAsync(
            evento: EventoSucesso,
            entidade: EntidadeAuditoria,
            entidadeId: null,
            dados: null,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        _log.LogInformation("Logout efetuado. UsuarioId={UsuarioId}", _contexto.UsuarioId);

        return new LogoutResultado();
    }
}
