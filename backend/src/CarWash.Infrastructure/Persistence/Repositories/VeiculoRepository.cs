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

    /// <inheritdoc/>
    public Task<bool> ExistePlacaAsync(string placaNormalizada, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(placaNormalizada);

        return _context.Veiculos
            .AsNoTracking()
            .AnyAsync(v => v.Placa == placaNormalizada, cancellationToken);
    }

    /// <inheritdoc/>
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

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException pg
            && pg.SqlState == PostgresErrorCodes.UniqueViolation;
    }
}
