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

    public Task<bool> ExistePlacaExcetoAsync(string placaNormalizada, Guid ignoreVeiculoId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(placaNormalizada);

        return _context.Veiculos
            .AsNoTracking()
            .AnyAsync(v => v.Placa == placaNormalizada && v.Id != ignoreVeiculoId, cancellationToken);
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

    public Task<Veiculo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _context.Veiculos
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<Veiculo> Itens, int Total)> ListarPorClienteIdAsync(
        Guid clienteId,
        string? busca,
        bool? ativo,
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken)
    {
        if (pagina < 1)
        {
            pagina = 1;
        }

        if (tamanhoPagina < 1)
        {
            tamanhoPagina = 20;
        }

        if (tamanhoPagina > 100)
        {
            tamanhoPagina = 100;
        }

        var query = _context.Veiculos
            .AsNoTracking()
            .Where(v => v.ClienteId == clienteId);

        if (ativo.HasValue)
        {
            query = query.Where(v => v.Ativo == ativo.Value);
        }

        if (!string.IsNullOrWhiteSpace(busca))
        {
            string termo = $"%{busca.Trim().ToUpperInvariant()}%";
            query = query.Where(v =>
                EF.Functions.ILike(v.Placa, termo)
                || EF.Functions.ILike(v.Modelo, termo)
                || EF.Functions.ILike(v.Fabricante, termo));
        }

        int total = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var itens = await query
            .OrderBy(v => v.Placa)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (itens, total);
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

    public async Task SalvarAsync(CancellationToken cancellationToken)
    {
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
