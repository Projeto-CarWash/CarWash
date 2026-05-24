using System.Text.Json;
using CarWash.Application.Servicos.Common;
using CarWash.Application.Servicos.Persistence;
using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PostgresErrorCodes = Npgsql.PostgresErrorCodes;

namespace CarWash.Infrastructure.Persistence.Repositories;

public class ServicoRepository : IServicoRepository
{
    private readonly CarWashDbContext context;

    public ServicoRepository(CarWashDbContext context)
    {
        this.context = context;
    }

    public Task<bool> ExisteNomeAsync(string nome, Guid? ignoreServicoId, CancellationToken cancellationToken)
    {
        return context.Servicos
            .AsNoTracking()
            .AnyAsync(x => x.Nome == nome
                && (ignoreServicoId == null || x.Id != ignoreServicoId),
                cancellationToken);
    }

    public Task<Servico?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return context.Servicos
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AdicionarAsync(Servico servico, string correlationId, Guid? usuarioId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(servico);

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        await context.Servicos.AddAsync(servico, cancellationToken);

        var audit = AuditLog.Registrar(
            id: Guid.NewGuid(),
            evento: "SERVICO_CRIADO",
            entidade: "servicos",
            correlationId: correlationId,
            entidadeId: servico.Id,
            usuarioId: usuarioId,
            dados: JsonSerializer.Serialize(new
            {
                servico.Id,
                servico.Nome,
                servico.Preco,
                servico.DuracaoMin,
            }));

        await context.AuditLogs.AddAsync(audit, cancellationToken);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new NomeServicoJaExisteException(ex);
        }
    }

    public async Task SalvarAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new NomeServicoJaExisteException(ex);
        }
    }

    public async Task RegistrarAuditoriaAsync(
        string evento,
        Guid entidadeId,
        string correlationId,
        Guid? usuarioId,
        string? dados,
        CancellationToken cancellationToken)
    {
        var audit = AuditLog.Registrar(
            id: Guid.NewGuid(),
            evento: evento,
            entidade: "servicos",
            correlationId: correlationId,
            entidadeId: entidadeId,
            usuarioId: usuarioId,
            dados: dados);

        await context.AuditLogs.AddAsync(audit, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Servico> Itens, int Total)> ListarAsync(
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

        var query = context.Servicos.AsNoTracking();

        if (ativo.HasValue)
        {
            query = query.Where(x => x.Ativo == ativo.Value);
        }

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termoNormalizado = $"%{busca.Trim()}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.Nome, termoNormalizado));
        }

        var total = await query.CountAsync(cancellationToken);

        var itens = await query
            .OrderBy(x => x.Nome)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken);

        return (itens, total);
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
    }
}
