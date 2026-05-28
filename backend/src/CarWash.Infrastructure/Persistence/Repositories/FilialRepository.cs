using CarWash.Application.Filiais.Common;
using CarWash.Application.Filiais.Persistence;
using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CarWash.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação concreta de <see cref="IFilialRepository"/> sobre EF Core.
/// Mantém a Application desacoplada do <see cref="CarWashDbContext"/>.
/// Mesmo padrão do <see cref="UsuarioRepository"/>: traduz violação de UK do
/// PostgreSQL (SQLSTATE 23505) em <see cref="NomeFilialJaExisteException"/>.
/// </summary>
public sealed class FilialRepository : IFilialRepository
{
    /// <summary>Nome da UK que protege contra nome duplicado (FilialConfiguration).</summary>
    private const string ConstraintNomeUnico = "uk_filiais_nome";

    /// <summary>PostgreSQL: SQLSTATE para <c>unique_violation</c>.</summary>
    private const string PostgresUniqueViolationSqlState = "23505";

    private readonly CarWashDbContext _db;

    public FilialRepository(CarWashDbContext db)
    {
        _db = db;
    }

    public Task<Filial?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Filiais.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public Task<Filial?> ObterPorIdRastreadoAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Filiais.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public Task<bool> ExisteComNomeAsync(string nome, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        var alvo = nome.Trim();

        // ILIKE para case-insensitive — alinha o pré-check ao comportamento
        // esperado da UK quando o cliente envia o nome com caixa diferente.
        return _db.Filiais
            .AsNoTracking()
            .AnyAsync(f => EF.Functions.ILike(f.Nome, alvo), cancellationToken);
    }

    public Task AdicionarAsync(Filial filial, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filial);
        _db.Filiais.Add(filial);
        return Task.CompletedTask;
    }

    public async Task SalvarAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (IsNomeUniqueViolation(ex))
        {
            // Race condition na UK uk_filiais_nome — traduz para exceção da
            // Application, isolando EF/Npgsql nesta camada (mesmo padrão de
            // UsuarioRepository).
            throw new NomeFilialJaExisteException(ex);
        }
    }

    public Task<int?> ObterCelulasAtivasAsync(Guid filialId, CancellationToken cancellationToken) =>
        _db.Filiais
            .AsNoTracking()
            .Where(f => f.Id == filialId)
            .Select(f => (int?)f.CelulasAtivas)
            .FirstOrDefaultAsync(cancellationToken);

    private static bool IsNomeUniqueViolation(DbUpdateException ex)
    {
        if (ex.InnerException is not PostgresException pg)
        {
            return false;
        }

        return string.Equals(pg.SqlState, PostgresUniqueViolationSqlState, StringComparison.Ordinal)
            && pg.ConstraintName is not null
            && pg.ConstraintName.Contains(ConstraintNomeUnico, StringComparison.OrdinalIgnoreCase);
    }
}
