using System.Security.Cryptography;
using CarWash.Application.Abstractions;
using CarWash.Application.Auth.Abstractions;
using CarWash.Application.Auth.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Domain.Entities;
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

    private readonly IUsuarioSessaoRepository _repo;
    private readonly ITokenHasher _hasher;
    private readonly ICurrentRequestContext _contexto;
    private readonly JwtOptions _opcoes;

    public RefreshTokenService(
        IUsuarioSessaoRepository repo,
        ITokenHasher hasher,
        ICurrentRequestContext contexto,
        IOptions<JwtOptions> opcoes)
    {
        _repo = repo;
        _hasher = hasher;
        _contexto = contexto;
        _opcoes = opcoes.Value;
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

        if (sessao is null || !sessao.EstaAtiva(DateTime.UtcNow))
        {
            throw new RefreshTokenInvalidoException();
        }

        return sessao;
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
