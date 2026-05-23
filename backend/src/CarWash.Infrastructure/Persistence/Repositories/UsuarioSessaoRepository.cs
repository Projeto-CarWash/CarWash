using CarWash.Application.Auth.Persistence;
using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

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

    public async Task<UsuarioSessao?> ObterPorHashParaAtualizacaoAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenHash))
        {
            return null;
        }

        // SELECT ... FOR UPDATE serializa o acesso à linha. Sem isso, em READ
        // COMMITTED (default do PG), duas conexões simultâneas leem a mesma versão
        // pré-revogação e ambas concluem a rotação — BUG-010. Com o lock pessimista,
        // a segunda conexão bloqueia até o COMMIT da primeira (que setou
        // revogado_em); ao adquirir o lock, lê a versão atualizada e cai no caminho
        // de reuse-detection (CA011 / BUG-008).
        //
        // Importante: o lock só vive enquanto a transação atual estiver aberta. Por
        // isso este método deve ser chamado dentro de IniciarTransacaoAsync.
        //
        // Limitamos a 1 linha — refresh_token_hash é único na prática (SHA-256 de
        // RandomNumberGenerator 32B). EF Core materializa a entidade rastreada para
        // que o subsequente .Revogar() + SaveChanges encontre o estado.
        var sessoes = await _contexto.UsuarioSessoes
            .FromSqlInterpolated($"""
                SELECT * FROM public.usuario_sessoes
                WHERE refresh_token_hash = {refreshTokenHash}
                FOR UPDATE
                """)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return sessoes.FirstOrDefault();
    }

    public async Task<IUsuarioSessaoTransacao> IniciarTransacaoAsync(CancellationToken cancellationToken)
    {
        // Quando já existir uma transação ambiente (ex.: testes de integração que
        // envolvem todo o request em uma transação) devolvemos um handle no-op para
        // evitar BEGIN aninhado, que o Npgsql não suporta sem savepoint.
        if (_contexto.Database.CurrentTransaction is not null)
        {
            return new UsuarioSessaoTransacaoNoOp();
        }

        var transacao = await _contexto.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        return new UsuarioSessaoTransacaoEf(transacao);
    }

    public async Task AdicionarAsync(UsuarioSessao sessao, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sessao);
        await _contexto.UsuarioSessoes.AddAsync(sessao, cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> RevogarTodasAtivasDoUsuarioAsync(
        Guid usuarioId,
        DateTime quandoUtc,
        CancellationToken cancellationToken)
    {
        var sessoes = await _contexto.UsuarioSessoes
            .Where(s => s.UsuarioId == usuarioId
                        && s.RevogadoEm == null
                        && s.ExpiraEm > quandoUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (sessoes.Count == 0)
        {
            return 0;
        }

        foreach (var sessao in sessoes)
        {
            sessao.Revogar(quandoUtc);
        }

        await _contexto.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return sessoes.Count;
    }

    public Task SalvarAsync(CancellationToken cancellationToken)
        => _contexto.SaveChangesAsync(cancellationToken);

    /// <summary>Wrapper sobre <see cref="IDbContextTransaction"/> para a Application.</summary>
    private sealed class UsuarioSessaoTransacaoEf : IUsuarioSessaoTransacao
    {
        private readonly IDbContextTransaction _interno;
        private bool _disposed;

        public UsuarioSessaoTransacaoEf(IDbContextTransaction interno)
        {
            _interno = interno;
        }

        public Task CommitAsync(CancellationToken cancellationToken)
            => _interno.CommitAsync(cancellationToken);

        public Task RollbackAsync(CancellationToken cancellationToken)
            => _interno.RollbackAsync(cancellationToken);

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            await _interno.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Handle no-op para quando já existe transação ambiente. Commit/Rollback
    /// silenciosos — a transação real é controlada pelo escopo externo.
    /// </summary>
    private sealed class UsuarioSessaoTransacaoNoOp : IUsuarioSessaoTransacao
    {
        public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RollbackAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
