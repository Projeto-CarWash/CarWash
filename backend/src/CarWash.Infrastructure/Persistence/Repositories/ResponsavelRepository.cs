using System.Text.Json;
using CarWash.Application.Responsaveis.Common;
using CarWash.Application.Responsaveis.Persistence;
using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PostgresErrorCodes = Npgsql.PostgresErrorCodes;

namespace CarWash.Infrastructure.Persistence.Repositories;

public sealed class ResponsavelRepository : IResponsavelRepository
{
    private readonly CarWashDbContext _context;

    public ResponsavelRepository(CarWashDbContext context)
    {
        _context = context;
    }

    public Task<bool> ExisteDocumentoAsync(string documento, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documento);
        return _context.Responsaveis
            .AsNoTracking()
            .AnyAsync(r => r.Documento == documento, cancellationToken);
    }

    public async Task AdicionarAsync(Responsavel responsavel, string correlationId, Guid? usuarioId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(responsavel);

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        await _context.Responsaveis.AddAsync(responsavel, cancellationToken);

        var audit = AuditLog.Registrar(
            id: Guid.NewGuid(),
            evento: "RESPONSAVEL_CRIADO",
            entidade: "responsaveis",
            correlationId: correlationId,
            entidadeId: responsavel.Id,
            usuarioId: usuarioId,
            dados: JsonSerializer.Serialize(new
            {
                responsavel.Id,
                responsavel.ClienteTitularId,
                responsavel.Nome,
                Documento = MascararDocumento(responsavel.Documento),
                responsavel.GrauVinculo,
            }));

        await _context.AuditLogs.AddAsync(audit, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new DocumentoResponsavelJaExisteException(ex);
        }
    }

    public async Task<IReadOnlyList<Responsavel>> ListarPorClienteTitularIdAsync(Guid clienteTitularId, CancellationToken cancellationToken)
    {
        return await _context.Responsaveis
            .AsNoTracking()
            .Where(r => r.ClienteTitularId == clienteTitularId)
            .OrderBy(r => r.Nome)
            .ToListAsync(cancellationToken);
    }

    public async Task<Responsavel?> ObterPorIdRastreadoAsync(Guid id, Guid clienteTitularId, CancellationToken cancellationToken)
    {
        return await _context.Responsaveis
            .FirstOrDefaultAsync(r => r.Id == id && r.ClienteTitularId == clienteTitularId, cancellationToken);
    }

    public async Task SalvarAsync(string correlationId, Guid? usuarioId, CancellationToken cancellationToken)
    {
        var entries = _context.ChangeTracker.Entries<Responsavel>()
            .Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Modified)
            .ToList();

        foreach (var entry in entries)
        {
            var responsavel = entry.Entity;
            var audit = AuditLog.Registrar(
                id: Guid.NewGuid(),
                evento: "RESPONSAVEL_ATUALIZADO",
                entidade: "responsaveis",
                correlationId: correlationId,
                entidadeId: responsavel.Id,
                usuarioId: usuarioId,
                dados: JsonSerializer.Serialize(new
                {
                    responsavel.Id,
                    responsavel.ClienteTitularId,
                    responsavel.Nome,
                    Documento = MascararDocumento(responsavel.Documento),
                    responsavel.GrauVinculo,
                    responsavel.Ativo,
                }));

            await _context.AuditLogs.AddAsync(audit, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string? MascararDocumento(string? documento)
    {
        if (string.IsNullOrWhiteSpace(documento))
        {
            return null;
        }

        if (documento.Length <= 4)
        {
            return "****";
        }

        return $"***{documento[^4..]}";
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException pg
            && pg.SqlState == PostgresErrorCodes.UniqueViolation;
    }
}
