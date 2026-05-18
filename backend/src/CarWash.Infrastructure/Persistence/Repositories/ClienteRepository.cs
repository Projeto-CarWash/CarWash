using System.Text.Json;
using CarWash.Application.Clientes.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PostgresErrorCodes = Npgsql.PostgresErrorCodes;

namespace CarWash.Infrastructure.Persistence.Repositories;

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

    public Task<bool> ExisteEmailAsync(string email, Guid? ignoreClienteId, CancellationToken cancellationToken)
    {
        // Comparação case-insensitive via ILike do PostgreSQL — emails sempre são
        // normalizados em lower antes de persistir (InputNormalizer.EmailOrNull),
        // mas defendemos contra dados legados ou inserção fora do fluxo padrão.
        var alvo = email.ToLowerInvariant();
        return context.Clientes
            .AsNoTracking()
            .AnyAsync(
                x => x.Email != null
                    && EF.Functions.ILike(x.Email, alvo)
                    && (ignoreClienteId == null || x.Id != ignoreClienteId),
                cancellationToken);
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
            // BUG-FILTRO-BUSCA-IGNORADO + BUG-BUSCA-DADO-INSUSPEITO + GAP-UNACCENT-ASSIM:
            // - Trim + normalização de acentos no termo (assimetria fechada com o lado
            //   PT-BR dos dados — "joão" passa a casar "Joao Silva").
            // - Casa APENAS em (nome, cpf, cnpj). Removido o LIKE em email e cidade,
            //   que eram canais de "casamento insuspeito" do RAT da QA — listar.md.
            // - O termo entra parametrizado via EF.Functions.ILike — não há injeção SQL.
            // - Busca por documento exige que o termo seja "primariamente numérico"
            //   (apenas dígitos + separadores comuns ".-/ ") para evitar que strings
            //   alfanuméricas como "xyzabc123notexist" casem CPFs por substring fortuita.
            var termoOriginal = busca.Trim();
            var termoNormalizado = RemoverAcentos(termoOriginal);
            var likeNomeNormalizado = $"%{termoNormalizado}%";

            string? digitos = null;
            if (TermoPareceDocumento(termoOriginal))
            {
                var soDigitos = new string([.. termoOriginal.Where(char.IsDigit)]);
                if (soDigitos.Length >= 3)
                {
                    digitos = soDigitos;
                }
            }

            query = query.Where(x =>
                EF.Functions.ILike(CarWashDbContext.Unaccent(x.Nome), likeNomeNormalizado)
                || (digitos != null && x.Cpf != null && x.Cpf.Contains(digitos))
                || (digitos != null && x.Cnpj != null && x.Cnpj.Contains(digitos)));
        }

        var total = await query.CountAsync(cancellationToken);

        var itens = await query
            .OrderBy(x => x.Nome)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken);

        return (itens, total);
    }

    /// <summary>
    /// Decide se o termo informado é "primariamente numérico" — composto apenas
    /// de dígitos + separadores comuns de documentos (<c>.</c>, <c>-</c>, <c>/</c>,
    /// espaço). Termos alfanuméricos não são tratados como possíveis CPF/CNPJ
    /// (BUG-BUSCA-DADO-INSUSPEITO: evita que "xyzabc123notexist" case CPF por
    /// substring fortuita ao extrair "123" dos seus dígitos).
    /// </summary>
    private static bool TermoPareceDocumento(string termo)
    {
        if (string.IsNullOrEmpty(termo))
        {
            return false;
        }

        foreach (var c in termo)
        {
            if (!char.IsDigit(c) && c != '.' && c != '-' && c != '/' && c != ' ')
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Remove diacríticos (acentos/cedilha) do termo em memória, espelhando o
    /// comportamento do <c>unaccent()</c> do PostgreSQL no lado C#. Usado para
    /// normalizar o termo de busca antes de comparar com a coluna unaccent-ada.
    /// </summary>
    private static string RemoverAcentos(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var normalizado = input.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder(normalizado.Length);
        foreach (var c in normalizado)
        {
            var categoria = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (categoria != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
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
