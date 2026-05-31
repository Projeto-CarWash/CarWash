using CarWash.Application.Abstractions;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Auth.Abstractions;
using CarWash.Application.Auth.Persistence;
using Microsoft.Extensions.Logging;

namespace CarWash.Application.Auth.Logout;

/// <summary>
/// Use case de logout. Revoga a sessão correspondente ao refresh token recebido.
/// Idempotente — chamadas sem token / token inválido completam normalmente
/// (o cliente sempre apaga o cookie). Audita apenas quando há sessão real revogada
/// e nesse caso emite log <c>Information</c> com <c>UsuarioId</c> e <c>SessaoId</c>
/// reais (BUG-007 — o log nunca deve sair com <c>UsuarioId=null</c>).
/// </summary>
public sealed class LogoutHandler : ICommandHandler<LogoutCommand, LogoutResultado>
{
    public const string EventoSucesso = "UsuarioLogout";
    public const string EntidadeAuditoria = "UsuarioSessao";

    private readonly IRefreshTokenService _refreshTokens;
    private readonly IUsuarioSessaoRepository _sessoes;
    private readonly ITokenHasher _hasher;
    private readonly IAuditLogger _auditoria;
    private readonly ICurrentRequestContext _contexto;
    private readonly ILogger<LogoutHandler> _log;

    public LogoutHandler(
        IRefreshTokenService refreshTokens,
        IUsuarioSessaoRepository sessoes,
        ITokenHasher hasher,
        IAuditLogger auditoria,
        ICurrentRequestContext contexto,
        ILogger<LogoutHandler> log)
    {
        _refreshTokens = refreshTokens;
        _sessoes = sessoes;
        _hasher = hasher;
        _auditoria = auditoria;
        _contexto = contexto;
        _log = log;
    }

    /// <inheritdoc/>
    public async Task<LogoutResultado> HandleAsync(LogoutCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            // Sem cookie / cookie vazio: response 200 mesmo assim — cliente apaga o
            // cookie local. Não emitimos o log de sucesso (UsuarioId seria null).
            _log.LogDebug("Logout solicitado sem sessão ativa (cookie ausente).");
            return new LogoutResultado();
        }

        // Resolver a sessão ANTES de revogar — assim conseguimos UsuarioId/SessaoId
        // mesmo que o endpoint seja anônimo (sem JWT). Se a sessão não existe
        // (token forjado/já apagado), tratamos como idempotente: sem log de sucesso.
        string hash = _hasher.Hash(command.RefreshToken);
        var sessao = await _sessoes.ObterPorHashAsync(hash, cancellationToken).ConfigureAwait(false);

        if (sessao is null)
        {
            _log.LogDebug("Logout solicitado sem sessão ativa (refresh token desconhecido).");
            return new LogoutResultado();
        }

        await _refreshTokens.RevogarAsync(command.RefreshToken, cancellationToken).ConfigureAwait(false);

        _contexto.DefinirEvento(EventoSucesso);
        await _auditoria.LogAsync(
            evento: EventoSucesso,
            entidade: EntidadeAuditoria,
            entidadeId: sessao.Id,
            dados: null,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        _log.LogInformation(
            "Logout efetuado. UsuarioId={UsuarioId}, SessaoId={SessaoId}",
            sessao.UsuarioId,
            sessao.Id);

        return new LogoutResultado();
    }
}
