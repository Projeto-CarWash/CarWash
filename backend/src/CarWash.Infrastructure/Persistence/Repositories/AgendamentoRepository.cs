using System.Text.Json;
using CarWash.Application.Agendamentos.Common;
using CarWash.Application.Agendamentos.Persistence;
using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CarWash.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação concreta de <see cref="IAgendamentoRepository"/> sobre EF Core.
/// Persiste o agregado (agendamento + itens + histórico) em transação única e
/// traduz a violação da constraint EXCLUDE <c>ex_ag_veiculo_janela</c> (RN011)
/// em <see cref="AgendamentoConflitanteException"/>.
/// </summary>
public sealed class AgendamentoRepository : IAgendamentoRepository
{
    /// <summary>PostgreSQL: SQLSTATE para <c>exclusion_violation</c>.</summary>
    private const string ExclusionViolationSqlState = "23P01";

    /// <summary>Prefixo das constraints EXCLUDE de conflito de veículo (RN011).</summary>
    private const string ConstraintConflitoVeiculoPrefixo = "ex_ag_veiculo";

    private readonly CarWashDbContext _db;

    public AgendamentoRepository(CarWashDbContext db)
    {
        _db = db;
    }

    public Task<bool> ExisteConflitoVeiculoAsync(
        Guid veiculoId,
        DateTime inicio,
        DateTime fim,
        CancellationToken cancellationToken)
    {
        // Espelha a EXCLUDE ex_ag_veiculo_janela: janela meio-aberta [inicio, fim),
        // apenas status 'agendado'. Sobreposição ⇔ inicio_existente < fim_novo
        // && fim_existente > inicio_novo.
        return _db.Agendamentos
            .AsNoTracking()
            .AnyAsync(
                a => a.VeiculoId == veiculoId
                    && a.StatusRaw == "agendado"
                    && a.Inicio < fim
                    && a.Fim > inicio,
                cancellationToken);
    }

    public async Task AdicionarAsync(
        Agendamento agendamento,
        IReadOnlyCollection<AgendamentoItem> itens,
        AgendamentoHistorico historico,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(agendamento);
        ArgumentNullException.ThrowIfNull(itens);
        ArgumentNullException.ThrowIfNull(historico);

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await _db.Agendamentos.AddAsync(agendamento, cancellationToken).ConfigureAwait(false);
        await _db.AgendamentoItens.AddRangeAsync(itens, cancellationToken).ConfigureAwait(false);
        await _db.AgendamentoHistoricos.AddAsync(historico, cancellationToken).ConfigureAwait(false);

        var audit = AuditLog.Registrar(
            id: Guid.NewGuid(),
            evento: "AGENDAMENTO_CRIADO",
            entidade: "agendamentos",
            correlationId: correlationId,
            entidadeId: agendamento.Id,
            usuarioId: agendamento.CriadoPor,
            dados: JsonSerializer.Serialize(new
            {
                agendamento.Id,
                agendamento.FilialId,
                agendamento.VeiculoId,
                agendamento.ClienteId,
                agendamento.ResponsavelId,
                agendamento.Inicio,
                agendamento.Fim,
                agendamento.DuracaoTotalMin,
                agendamento.ValorTotal,
                QtdServicos = itens.Count,
            }));
        await _db.AuditLogs.AddAsync(audit, cancellationToken).ConfigureAwait(false);

        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (IsConflitoVeiculoViolation(ex))
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw new AgendamentoConflitanteException(ex);
        }
    }

    /// <summary>
    /// Detecta a violação da constraint EXCLUDE de conflito de veículo (RN011).
    /// Reconhece tanto pelo SQLSTATE <c>23P01</c> (exclusion_violation) quanto
    /// pelo nome da constraint começar com <c>ex_ag_veiculo</c> — cobre a
    /// <c>ex_ag_veiculo_janela</c> criada na <c>InitialSchema</c>.
    /// </summary>
    private static bool IsConflitoVeiculoViolation(DbUpdateException exception)
    {
        if (exception.InnerException is not PostgresException pg)
        {
            return false;
        }

        var ehExclusionViolation = string.Equals(
            pg.SqlState,
            ExclusionViolationSqlState,
            StringComparison.Ordinal);

        var ehConstraintDeVeiculo = pg.ConstraintName is { } nome
            && nome.Contains(ConstraintConflitoVeiculoPrefixo, StringComparison.OrdinalIgnoreCase);

        return ehExclusionViolation || ehConstraintDeVeiculo;
    }
}
