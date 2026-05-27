using CarWash.Application.Common.Exceptions;
using CarWash.Application.Filiais.Common;
using CarWash.Application.Filiais.Persistence;
using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PostgresErrorCodes = Npgsql.PostgresErrorCodes;

namespace CarWash.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do <see cref="IFilialRepository"/>. Traduz
/// <see cref="DbUpdateException"/> por <see cref="PostgresException.ConstraintName"/>
/// em exceções específicas (ADR-0007 §5.2) para o middleware global responder
/// 409 com o slug correto.
/// </summary>
public class FilialRepository : IFilialRepository
{
    private const string ConstraintCodigo = "uk_filiais_codigo";
    private const string ConstraintCnpj = "uk_filiais_cnpj";
    private const string ConstraintNomeLower = "uk_filiais_nome_lower";

    private readonly CarWashDbContext context;

    public FilialRepository(CarWashDbContext context)
    {
        this.context = context;
    }

    public Task<bool> ExisteCodigoAsync(string codigo, CancellationToken cancellationToken)
    {
        return context.Filiais
            .AsNoTracking()
            .AnyAsync(x => x.Codigo == codigo, cancellationToken);
    }

    public Task<bool> ExisteCnpjAsync(string cnpj, CancellationToken cancellationToken)
    {
        return context.Filiais
            .AsNoTracking()
            .AnyAsync(x => x.Cnpj == cnpj, cancellationToken);
    }

    public Task<bool> ExisteNomeAsync(string nome, CancellationToken cancellationToken)
    {
        // ILike (PostgreSQL) é case-insensitive e traduzível para SQL via Npgsql.
        // Sem wildcards, equivale a igualdade case-insensitive — consistente com
        // o índice funcional uk_filiais_nome_lower (LOWER(nome)).
        return context.Filiais
            .AsNoTracking()
            .AnyAsync(x => EF.Functions.ILike(x.Nome, nome), cancellationToken);
    }

    public Task<Filial?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return context.Filiais.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AdicionarAsync(Filial filial, string correlationId, Guid? usuarioId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filial);

        await context.Filiais.AddAsync(filial, cancellationToken).ConfigureAwait(false);

        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Race condition: outro POST concorrente venceu o pré-check.
            // Inspeciona o ConstraintName para escolher o slug certo.
            var constraintName = (ex.InnerException as PostgresException)?.ConstraintName;
            throw constraintName switch
            {
                ConstraintCodigo => new FilialCodigoJaExisteException(ex),
                ConstraintCnpj => new FilialCnpjJaExisteException(ex),
                ConstraintNomeLower => new FilialNomeJaExisteException(ex),
                _ => new ConflictException("Conflito ao cadastrar filial.", ex),
            };
        }
    }

    public async Task<(IReadOnlyList<Filial> Itens, int Total)> ListarAsync(
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

        var query = context.Filiais.AsNoTracking();

        if (ativo.HasValue)
        {
            query = query.Where(x => x.Ativa == ativo.Value);
        }

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            var like = $"%{termo}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.Nome, like)
                || (x.Codigo != null && EF.Functions.ILike(x.Codigo, like))
                || (x.EnderecoCidade != null && EF.Functions.ILike(x.EnderecoCidade, like)));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var itens = await query
            .OrderBy(x => x.Nome)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return (itens, total);
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
    }
}
