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

    /// <summary>
    /// SQLSTATEs de concorrência do PostgreSQL — <c>deadlock_detected</c> (40P01) e
    /// <c>serialization_failure</c> (40001). Sob dois INSERTs concorrentes na mesma
    /// janela, a constraint EXCLUDE GiST pode abortar a transação perdedora por
    /// deadlock em vez de <c>exclusion_violation</c>; o desfecho da RN011 é o mesmo —
    /// a transação não persistiu — então o resultado correto é 409, não 500.
    /// </summary>
    private static readonly string[] ConcorrenciaSqlStates = ["40P01", "40001"];

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
    /// Detecta o conflito de janela de veículo (RN011) durante a persistência.
    /// Reconhece o SQLSTATE <c>23P01</c> (exclusion_violation), o nome da constraint
    /// começar com <c>ex_ag_veiculo</c> e ainda os SQLSTATEs de concorrência
    /// (<c>40P01</c>/<c>40001</c>): dois INSERTs simultâneos na mesma janela podem
    /// abortar a transação perdedora por deadlock — o desfecho da RN011 é idêntico.
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

        var ehConcorrencia = Array.Exists(
            ConcorrenciaSqlStates,
            estado => string.Equals(pg.SqlState, estado, StringComparison.Ordinal));

        return ehExclusionViolation || ehConstraintDeVeiculo || ehConcorrencia;
    }
}
