using CarWash.Application.Abstractions;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Auth.Abstractions;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Usuarios.Persistence;
using Microsoft.Extensions.Logging;

namespace CarWash.Application.Auth.Refresh;

/// <summary>
/// Use case de renovação de sessão. Rotação obrigatória:
/// <list type="number">
///   <item>Valida o refresh atual via <see cref="IRefreshTokenService.ValidarAsync"/>
///   (lança <see cref="RefreshTokenInvalidoException"/> → HTTP 401 se ausente/expirado/revogado).</item>
///   <item>Carrega o usuário; se inexistente, inativo ou bloqueado, revoga a
///   sessão atual e lança <see cref="RefreshTokenInvalidoException"/> (não revela motivo).</item>
///   <item>Revoga a sessão atual (rotação) e emite uma NOVA sessão + novo access JWT.</item>
///   <item>Audita o evento <see cref="EventoSucesso"/>.</item>
/// </list>
/// </summary>
public sealed class RefreshHandler : ICommandHandler<RefreshCommand, RefreshResultado>
{
    public const string EventoSucesso = "UsuarioSessaoRenovada";
    public const string EventoFalha = "UsuarioSessaoRefreshFalha";
    public const string EntidadeAuditoria = "UsuarioSessao";

    private readonly IRefreshTokenService _refreshTokens;
    private readonly IAccessTokenService _accessTokens;
    private readonly IUsuarioRepository _repositorio;
    private readonly IAuditLogger _auditoria;
    private readonly ICurrentRequestContext _contexto;
    private readonly ILogger<RefreshHandler> _log;

    public RefreshHandler(
        IRefreshTokenService refreshTokens,
        IAccessTokenService accessTokens,
        IUsuarioRepository repositorio,
        IAuditLogger auditoria,
        ICurrentRequestContext contexto,
        ILogger<RefreshHandler> log)
    {
        _refreshTokens = refreshTokens;
        _accessTokens = accessTokens;
        _repositorio = repositorio;
        _auditoria = auditoria;
        _contexto = contexto;
        _log = log;
    }

    /// <inheritdoc/>
    public async Task<RefreshResultado> HandleAsync(RefreshCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // BUG-010: ValidarParaRotacaoAsync abre uma transação dedicada com lock
        // pessimista sobre a linha da sessão. O lock garante que requisições
        // concorrentes com o MESMO refresh token sejam serializadas — apenas a
        // primeira segue adiante; as demais, ao adquirirem o lock após o COMMIT
        // desta, enxergam revogado_em preenchido e caem em reuse-detection
        // (CA011) com resposta 401. Lança RefreshTokenInvalidoException (ou
        // subclasse) já com rollback interno se o token estiver ausente, expirado,
        // revogado ou reuse. No caminho feliz, devolve o handle que commitamos
        // após emitir a nova sessão.
        var rotacao = await _refreshTokens
            .ValidarParaRotacaoAsync(command.RefreshToken, cancellationToken)
            .ConfigureAwait(false);

        await using (rotacao.Transacao.ConfigureAwait(false))
        {
            var sessaoAtual = rotacao.SessaoAtual;

            var usuario = await _repositorio.ObterPorIdAsync(sessaoAtual.UsuarioId, cancellationToken).ConfigureAwait(false);
            var agora = DateTime.UtcNow;

            if (usuario is null || !usuario.Ativo || usuario.EstaBloqueado(agora))
            {
                // Usuário desativado/excluído/bloqueado depois do login. Revoga a sessão
                // ativa e responde como token inválido (não vaza motivo).
                await _refreshTokens.RevogarAsync(command.RefreshToken, cancellationToken).ConfigureAwait(false);
                await rotacao.Transacao.CommitAsync(cancellationToken).ConfigureAwait(false);

                _contexto.DefinirEvento(EventoFalha);
                await _auditoria.LogAsync(
                    evento: EventoFalha,
                    entidade: EntidadeAuditoria,
                    entidadeId: sessaoAtual.Id,
                    dados: new { Motivo = "UsuarioInvalido" },
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                throw new RefreshTokenInvalidoException();
            }

            // Rotação: revoga sessão atual e emite uma nova — tudo dentro da
            // transação iniciada pelo FOR UPDATE para que o lock persista até o COMMIT.
            await _refreshTokens.RevogarAsync(command.RefreshToken, cancellationToken).ConfigureAwait(false);

            var (accessToken, accessExpiresAt) = _accessTokens.Emitir(usuario);
            var novaSessao = await _refreshTokens.EmitirAsync(usuario, cancellationToken).ConfigureAwait(false);

            await rotacao.Transacao.CommitAsync(cancellationToken).ConfigureAwait(false);

            _contexto.DefinirEvento(EventoSucesso);
            await _auditoria.LogAsync(
                evento: EventoSucesso,
                entidade: EntidadeAuditoria,
                entidadeId: novaSessao.SessaoId,
                dados: new { SessaoAnteriorId = sessaoAtual.Id, SessaoNovaId = novaSessao.SessaoId },
                cancellationToken: cancellationToken).ConfigureAwait(false);

            _log.LogInformation(
                "Sessão renovada. UsuarioId={UsuarioId}, SessaoAnterior={SessaoAnterior}, SessaoNova={SessaoNova}",
                usuario.Id,
                sessaoAtual.Id,
                novaSessao.SessaoId);

            return new RefreshResultado(
                AccessToken: accessToken,
                AccessExpiresAt: accessExpiresAt,
                RefreshToken: novaSessao.RefreshToken,
                RefreshExpiresAt: novaSessao.ExpiraEm,
                Usuario: new RefreshResultado.UsuarioLogado(
                    usuario.Id,
                    usuario.Nome,
                    usuario.EmailValor,
                    usuario.Perfil));
        }
    }
}
