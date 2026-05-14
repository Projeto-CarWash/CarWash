using System.Text.Json;
using CarWash.Application.Interfaces;
using CarWash.Domain.Entities;
using CarWash.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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

    public async Task AdicionarAsync(Cliente cliente, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        await context.Clientes.AddAsync(cliente, cancellationToken);

        var audit = new AuditLog(
            evento: "CLIENTE_CRIADO",
            entidade: "clientes",
            entidadeId: cliente.Id,
            usuarioId: null,
            correlationId: "repository",
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

        await context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
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
}
