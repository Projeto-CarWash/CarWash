using System.Security.Cryptography;
using CarWash.Application.Abstractions;
using CarWash.Application.Auth.Abstractions;
using CarWash.Application.Auth.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarWash.Infrastructure.Auth;

/// <summary>
/// Ciclo de vida do refresh token. Persiste sessão em <c>usuario_sessoes</c>
/// com SHA-256 (<see cref="ITokenHasher"/>), IP/UA do <see cref="ICurrentRequestContext"/>,
/// e expiração configurada em <see cref="JwtOptions.RefreshTokenValidade"/>.
/// </summary>
public sealed class RefreshTokenService : IRefreshTokenService
{
    public const int TamanhoBytes = 32;

    public const string EventoFamiliaRevogadaPorReuse = "UsuarioSessaoFamiliaRevogadaPorReuse";
    public const string EntidadeAuditoria = "UsuarioSessao";

    private readonly IUsuarioSessaoRepository _repo;
    private readonly ITokenHasher _hasher;
    private readonly ICurrentRequestContext _contexto;
    private readonly IAuditLogger _auditoria;
    private readonly JwtOptions _opcoes;
    private readonly ILogger<RefreshTokenService> _log;

    public RefreshTokenService(
        IUsuarioSessaoRepository repo,
        ITokenHasher hasher,
        ICurrentRequestContext contexto,
        IAuditLogger auditoria,
        IOptions<JwtOptions> opcoes,
        ILogger<RefreshTokenService> log)
    {
        _repo = repo;
        _hasher = hasher;
        _contexto = contexto;
        _auditoria = auditoria;
        _opcoes = opcoes.Value;
        _log = log;
    }

    public async Task<RefreshTokenEmitido> EmitirAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        cancellationToken.ThrowIfCancellationRequested();

        var tokenBruto = GerarTokenBruto();
        var tokenHash = _hasher.Hash(tokenBruto);
        var expira = DateTime.UtcNow.Add(_opcoes.RefreshTokenValidade);

        var sessao = UsuarioSessao.Criar(
            id: Guid.NewGuid(),
            usuarioId: usuario.Id,
            refreshTokenHash: tokenHash,
            expiraEm: expira,
            ipOrigem: _contexto.IpOrigem,
            userAgent: _contexto.UserAgent);

        await _repo.AdicionarAsync(sessao, cancellationToken).ConfigureAwait(false);
        await _repo.SalvarAsync(cancellationToken).ConfigureAwait(false);

        return new RefreshTokenEmitido(tokenBruto, expira, sessao.Id);
    }

    public async Task<UsuarioSessao> ValidarAsync(string refreshTokenBruto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenBruto))
        {
            throw new RefreshTokenInvalidoException();
        }

        var hash = _hasher.Hash(refreshTokenBruto);
        var sessao = await _repo.ObterPorHashAsync(hash, cancellationToken).ConfigureAwait(false);

        await AvaliarOuLancarAsync(sessao, cancellationToken).ConfigureAwait(false);

        return sessao!;
    }

    public async Task<RotacaoContexto> ValidarParaRotacaoAsync(
        string refreshTokenBruto,
        CancellationToken cancellationToken)
    {
        // Validação rápida do input antes de abrir transação — não vale gastar
        // BEGIN/COMMIT para um cookie vazio.
        if (string.IsNullOrWhiteSpace(refreshTokenBruto))
        {
            throw new RefreshTokenInvalidoException();
        }

        var hash = _hasher.Hash(refreshTokenBruto);

        var transacao = await _repo.IniciarTransacaoAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Lock pessimista (FOR UPDATE) sobre a linha da sessão. Em concorrência
            // entre dois /refresh com o mesmo cookie:
            //  - Conexão A adquire o lock, valida, revoga (UPDATE pendente), commita.
            //  - Conexão B fica bloqueada no SELECT até o COMMIT de A; ao seguir,
            //    enxerga revogado_em preenchido e cai no caminho de reuse-detection
            //    (CA011) — retorna 401 e revoga a família por segurança.
            // Sem o FOR UPDATE, ambas leem a versão pré-revogação simultaneamente
            // e emitem refresh tokens distintos com sucesso — exatamente o BUG-010.
            var sessao = await _repo.ObterPorHashParaAtualizacaoAsync(hash, cancellationToken)
                .ConfigureAwait(false);

            try
            {
                await AvaliarOuLancarAsync(sessao, cancellationToken).ConfigureAwait(false);
            }
            catch (RefreshTokenReuseDetectadoException)
            {
                // Reuse-detection: o UPDATE de revogação da família é parte da
                // transação aberta aqui. PRECISAMOS commitar antes de lançar para
                // não desfazer a revogação no Dispose. Sem isso, o BUG-008 fix
                // é silenciosamente revertido pelo rollback.
                await transacao.CommitAsync(cancellationToken).ConfigureAwait(false);
                await transacao.DisposeAsync().ConfigureAwait(false);
                throw;
            }

            // Caller commita após emitir nova sessão (ou disposer p/ rollback).
            return new RotacaoContexto(sessao!, transacao);
        }
        catch (RefreshTokenReuseDetectadoException)
        {
            // Já commitamos e disposamos no handler interno acima — apenas relançar.
            throw;
        }
        catch
        {
            // Demais exceções (token inválido/expirado, falha de IO, etc.) liberam
            // o handle sem commit → rollback no IDbContextTransaction subjacente.
            await transacao.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Aplica as regras de validação compartilhadas entre <see cref="ValidarAsync"/>
    /// e <see cref="ValidarParaRotacaoAsync"/>: reuse-detection (CA011),
    /// sessão revogada/expirada → <see cref="RefreshTokenInvalidoException"/>.
    /// </summary>
    private async Task AvaliarOuLancarAsync(UsuarioSessao? sessao, CancellationToken cancellationToken)
    {
        if (sessao is null)
        {
            throw new RefreshTokenInvalidoException();
        }

        var agora = DateTime.UtcNow;

        // Reuse detection (CA011 / OAuth 2.1): o hash existe mas a sessão já foi
        // revogada e o token ainda não expirou por tempo. Sinal forte de que um
        // refresh já usado está sendo reapresentado — possível exfiltração. Revoga
        // TODAS as sessões ativas do usuário e responde como token inválido.
        if (sessao.RevogadoEm is not null && sessao.ExpiraEm > agora)
        {
            var afetadas = await _repo.RevogarTodasAtivasDoUsuarioAsync(sessao.UsuarioId, agora, cancellationToken)
                .ConfigureAwait(false);

            _log.LogWarning(
                "Refresh token reuse detectado. Família revogada por segurança. UsuarioId={UsuarioId}, SessaoComprometida={SessaoId}, SessoesAfetadas={Afetadas}",
                sessao.UsuarioId,
                sessao.Id,
                afetadas);

            _contexto.DefinirEvento(EventoFamiliaRevogadaPorReuse);
            await _auditoria.LogAsync(
                evento: EventoFamiliaRevogadaPorReuse,
                entidade: EntidadeAuditoria,
                entidadeId: sessao.Id,
                dados: new
                {
                    Motivo = "ReuseDetectado",
                    UsuarioId = sessao.UsuarioId,
                    SessoesAfetadas = afetadas,
                },
                cancellationToken: cancellationToken).ConfigureAwait(false);

            throw new RefreshTokenReuseDetectadoException(sessao.UsuarioId, sessao.Id, afetadas);
        }

        if (!sessao.EstaAtiva(agora))
        {
            throw new RefreshTokenInvalidoException();
        }
    }

    public async Task RevogarAsync(string refreshTokenBruto, CancellationToken cancellationToken)
    {
        // Idempotente: revogação com token inválido/inexistente não falha.
        if (string.IsNullOrWhiteSpace(refreshTokenBruto))
        {
            return;
        }

        var hash = _hasher.Hash(refreshTokenBruto);
        var sessao = await _repo.ObterPorHashAsync(hash, cancellationToken).ConfigureAwait(false);
        if (sessao is null)
        {
            return;
        }

        sessao.Revogar(DateTime.UtcNow);
        await _repo.SalvarAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string GerarTokenBruto()
    {
        var bytes = RandomNumberGenerator.GetBytes(TamanhoBytes);
        var b64 = Convert.ToBase64String(bytes);
        return b64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
