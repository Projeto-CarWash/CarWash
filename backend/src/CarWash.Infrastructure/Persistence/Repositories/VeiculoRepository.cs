using CarWash.Application.Veiculos.Common;
using CarWash.Application.Veiculos.Persistence;
using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PostgresErrorCodes = Npgsql.PostgresErrorCodes;

namespace CarWash.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do <see cref="IVeiculoRepository"/>. Defesa em
/// profundidade da RN011: pré-check + tradução da violação da UK
/// <c>uk_veiculos_placa</c> em <see cref="PlacaJaCadastradaException"/>.
/// </summary>
public sealed class VeiculoRepository : IVeiculoRepository
{
    private readonly CarWashDbContext _context;

    public VeiculoRepository(CarWashDbContext context)
    {
        _context = context;
    }

    public Task<bool> ExistePlacaAsync(string placaNormalizada, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(placaNormalizada);

        return _context.Veiculos
            .AsNoTracking()
            .AnyAsync(v => v.Placa == placaNormalizada, cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> PlacasExistentesAsync(
        IEnumerable<string> placasNormalizadas, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(placasNormalizadas);

        var lista = placasNormalizadas.ToList();

        if (lista.Count == 0)
        {
            return Array.Empty<string>();
        }

        var existentes = await _context.Veiculos
            .AsNoTracking()
            .Where(v => lista.Contains(v.Placa))
            .Select(v => v.Placa)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return existentes;
    }

    public async Task AdicionarAsync(Veiculo veiculo, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(veiculo);

        await _context.Veiculos.AddAsync(veiculo, cancellationToken).ConfigureAwait(false);

        try
        {
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new PlacaJaCadastradaException(ex);
        }
    }

    public async Task AdicionarRangeAsync(IEnumerable<Veiculo> veiculos, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(veiculos);

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await _context.Veiculos.AddRangeAsync(veiculos, cancellationToken).ConfigureAwait(false);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw new PlacaJaCadastradaException(ex);
        }
#pragma warning disable CA1031
        catch (Exception)
#pragma warning restore CA1031
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException pg
            && pg.SqlState == PostgresErrorCodes.UniqueViolation;
    }
}
