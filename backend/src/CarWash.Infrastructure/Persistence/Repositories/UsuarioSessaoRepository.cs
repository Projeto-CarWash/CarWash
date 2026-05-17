using CarWash.Application.Auth.Persistence;
using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarWash.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório EF Core para <see cref="UsuarioSessao"/>. Append + lookup
/// pelo SHA-256 do refresh token (índice <c>idx_usuario_sessoes_hash</c>).
/// </summary>
public sealed class UsuarioSessaoRepository : IUsuarioSessaoRepository
{
    private readonly CarWashDbContext _contexto;

    public UsuarioSessaoRepository(CarWashDbContext contexto)
    {
        _contexto = contexto;
    }

    public Task<UsuarioSessao?> ObterPorHashAsync(string refreshTokenHash, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenHash))
        {
            return Task.FromResult<UsuarioSessao?>(null);
        }

        return _contexto.UsuarioSessoes
            .FirstOrDefaultAsync(s => s.RefreshTokenHash == refreshTokenHash, cancellationToken);
    }

    public async Task AdicionarAsync(UsuarioSessao sessao, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sessao);
        await _contexto.UsuarioSessoes.AddAsync(sessao, cancellationToken).ConfigureAwait(false);
    }

    public Task SalvarAsync(CancellationToken cancellationToken)
        => _contexto.SaveChangesAsync(cancellationToken);
}
