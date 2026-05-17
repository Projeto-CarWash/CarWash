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
                cliente.EnderecoCidade,
                cliente.EnderecoUf,
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

    public Task SalvarAsync(CancellationToken cancellationToken)
        => context.SaveChangesAsync(cancellationToken);

    public async Task<(IReadOnlyList<Cliente> Itens, int Total)> ListarAsync(
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

        var query = context.Clientes.AsNoTracking();

        if (ativo.HasValue)
        {
            query = query.Where(x => x.Ativo == ativo.Value);
        }

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            var digitos = new string([.. termo.Where(char.IsDigit)]);
            var like = $"%{termo}%";

            query = query.Where(x =>
                EF.Functions.ILike(x.Nome, like)
                || (x.Email != null && EF.Functions.ILike(x.Email, like))
                || EF.Functions.ILike(x.EnderecoCidade, like)
                || (digitos.Length > 0 && x.Cpf != null && x.Cpf.Contains(digitos))
                || (digitos.Length > 0 && x.Cnpj != null && x.Cnpj.Contains(digitos))
                || (digitos.Length > 0 && x.Celular.Contains(digitos)));
        }

        var total = await query.CountAsync(cancellationToken);

        var itens = await query
            .OrderBy(x => x.Nome)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken);

        return (itens, total);
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
