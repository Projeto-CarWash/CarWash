using System.Text.Json;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Interfaces;
using CarWash.Domain.Entities;
using CarWash.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PostgresErrorCodes = Npgsql.PostgresErrorCodes;

namespace CarWash.Infrastructure.Repositories;

public class ClienteRepository : IClienteRepository
{
    private readonly CarWashDbContext context;

    public ClienteRepository(CarWashDbContext context)
    {
        this.context = context;
    }

    public Task<bool> ExisteCpfAsync(string cpf, CancellationToken cancellationToken)
    {
        return context.Clientes
            .AsNoTracking()
            .AnyAsync(x => x.Cpf == cpf, cancellationToken);
    }

    public Task<bool> ExisteCnpjAsync(string cnpj, CancellationToken cancellationToken)
    {
        return context.Clientes
            .AsNoTracking()
            .AnyAsync(x => x.Cnpj == cnpj, cancellationToken);
    }

    public Task<Cliente?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return context.Clientes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AdicionarAsync(Cliente cliente, string correlationId, Guid? usuarioId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cliente);

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        await context.Clientes.AddAsync(cliente, cancellationToken);

        var audit = AuditLog.Registrar(
            id: Guid.NewGuid(),
            evento: "CLIENTE_CRIADO",
            entidade: "clientes",
            correlationId: correlationId,
            entidadeId: cliente.Id,
            usuarioId: usuarioId,
            dados: JsonSerializer.Serialize(new
            {
                cliente.Id,
                cliente.Nome,
                Documento = MascararDocumento(cliente.Cpf ?? cliente.Cnpj),
                PossuiEmail = !string.IsNullOrWhiteSpace(cliente.Email),
                PossuiTelefone = !string.IsNullOrWhiteSpace(cliente.Telefone),
                PossuiCelular = !string.IsNullOrWhiteSpace(cliente.Celular),
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
            throw new ConflictException(
                "Já existe cliente cadastrado com este documento.",
                "cliente-documento-duplicado",
                ex);
        }
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
        return exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
    }
}
