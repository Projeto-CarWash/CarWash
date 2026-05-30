using CarWash.Domain.Entities;

namespace CarWash.Application.Auth.Abstractions;

/// <summary>
/// Gerencia o ciclo de vida do refresh token (RF001 — sessão com expiração e
/// invalidação em logout). Persiste cada token como SHA-256 em
/// <c>usuario_sessoes.refresh_token_hash</c> (ADR 0002 §Implicações).
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>
    /// Gera um refresh token bruto (Base64Url 32B), persiste a sessão com
    /// hash SHA-256 + IP/UA do request context, e retorna o token bruto que
    /// deve ser entregue ao cliente (apenas via Set-Cookie httpOnly).
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    Task<RefreshTokenEmitido> EmitirAsync(Usuario usuario, CancellationToken cancellationToken);

    /// <summary>
    /// Valida o refresh token bruto contra o hash armazenado em <c>usuario_sessoes</c>.
    /// Lança <see cref="Common.Exceptions.RefreshTokenInvalidoException"/> se ausente,
    /// expirado ou revogado.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    Task<UsuarioSessao> ValidarAsync(string refreshTokenBruto, CancellationToken cancellationToken);

    /// <summary>
    /// Valida o refresh token bruto sob <c>SELECT ... FOR UPDATE</c> em transação
    /// dedicada, prevenindo a race condition do BUG-010 em <c>/refresh</c> paralelo.
    /// Retorna a sessão validada + o handle de transação — que o chamador DEVE
    /// commitar após emitir a nova sessão (ou disposer para rollback). Se a sessão
    /// for inválida/revogada/expirada, faz rollback interno e lança
    /// <see cref="Common.Exceptions.RefreshTokenInvalidoException"/>; se for reuse
    /// (revogada e ainda não expirada), revoga família dentro da transação,
    /// commita e lança <see cref="Common.Exceptions.RefreshTokenReuseDetectadoException"/>.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    Task<RotacaoContexto> ValidarParaRotacaoAsync(string refreshTokenBruto, CancellationToken cancellationToken);

    /// <summary>
    /// Revoga (marca <c>revogado_em</c>) a sessão correspondente ao refresh token
    /// fornecido. Idempotente — chamadas com token inválido/já revogado não falham.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    Task RevogarAsync(string refreshTokenBruto, CancellationToken cancellationToken);
}

/// <summary>
/// Resultado da emissão de um refresh token. <c>RefreshToken</c> é o valor BRUTO
/// (não-hashado) que deve ir apenas pelo Set-Cookie httpOnly do response.
/// </summary>
public sealed record RefreshTokenEmitido(string RefreshToken, DateTime ExpiraEm, Guid SessaoId);
