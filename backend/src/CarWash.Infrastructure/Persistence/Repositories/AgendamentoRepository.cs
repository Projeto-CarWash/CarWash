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

    /// <summary>PostgreSQL: SQLSTATE para <c>unique_violation</c>.</summary>
    private const string UniqueViolationSqlState = "23505";

    /// <summary>Prefixo das constraints EXCLUDE de conflito de veículo (RN011).</summary>
    private const string ConstraintConflitoVeiculoPrefixo = "ex_ag_veiculo";

    /// <summary>UNIQUE da idempotência de confirmação (RF015).</summary>
    private const string ConstraintIdempotenciaKeyEscopo = "uq_idempotencia_key_escopo";

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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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
#pragma warning disable CA1031 // Toda falha de persistência é reavaliada como conflito da RN011 antes de subir.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            if (ex is OperationCanceledException)
            {
                throw;
            }

            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);

            if (await FalhaIndicaConflitoVeiculoAsync(ex, agendamento, cancellationToken).ConfigureAwait(false))
            {
                throw new AgendamentoConflitanteException(ex);
            }

            throw;
        }
    }

    /// <summary>
    /// Determina se uma falha de persistência foi, na verdade, a perda da corrida da
    /// RN011. Reconhece a violação direta da EXCLUDE pelo SQLSTATE
    /// (<see cref="IsConflitoVeiculoViolation"/>); para qualquer outra falha — deadlock,
    /// erro em cascata, exceção não encapsulada pelo EF — reconsulta a agenda do veículo
    /// (a transação já foi revertida). Havendo agendamento conflitante persistido, a
    /// falha é um conflito (409). Erro na própria releitura devolve <c>false</c>.
    /// </summary>
    private async Task<bool> FalhaIndicaConflitoVeiculoAsync(
        Exception falha,
        Agendamento agendamento,
        CancellationToken cancellationToken)
    {
        if (falha is DbUpdateException due && IsConflitoVeiculoViolation(due))
        {
            return true;
        }

        try
        {
            return await ExisteConflitoVeiculoAsync(
                agendamento.VeiculoId,
                agendamento.Inicio,
                agendamento.Fim,
                cancellationToken).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Falha na releitura não deve mascarar a exceção original.
        catch (Exception)
#pragma warning restore CA1031
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<ResultadoConfirmacaoIdempotente> AdicionarComIdempotenciaAsync(
        Agendamento agendamento,
        IReadOnlyCollection<AgendamentoItem> itens,
        AgendamentoHistorico historico,
        IdempotenciaRequisicao idempotencia,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(agendamento);
        ArgumentNullException.ThrowIfNull(itens);
        ArgumentNullException.ThrowIfNull(historico);
        ArgumentNullException.ThrowIfNull(idempotencia);

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await _db.Agendamentos.AddAsync(agendamento, cancellationToken).ConfigureAwait(false);
        await _db.AgendamentoItens.AddRangeAsync(itens, cancellationToken).ConfigureAwait(false);
        await _db.AgendamentoHistoricos.AddAsync(historico, cancellationToken).ConfigureAwait(false);
        await _db.IdempotenciaRequisicoes.AddAsync(idempotencia, cancellationToken).ConfigureAwait(false);

        var audit = AuditLog.Registrar(
            id: Guid.NewGuid(),
            evento: "AGENDAMENTO_CONFIRMADO",
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
                idempotencia.IdempotencyKey,
            }));
        await _db.AuditLogs.AddAsync(audit, cancellationToken).ConfigureAwait(false);

        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return ResultadoConfirmacaoIdempotente.Persistido();
        }
#pragma warning disable CA1031 // Toda falha de persistência é reavaliada (idempotência/RN011) antes de subir.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            if (ex is OperationCanceledException)
            {
                throw;
            }

            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);

            // Corrida de duas confirmações com a MESMA chave: a UNIQUE da idempotência
            // rejeitou esta transação. Relemos o registro vencedor num contexto limpo —
            // payload igual → replay; diferente → conflito.
            if (ex is DbUpdateException due && IsIdempotenciaViolation(due))
            {
                return await ResolverVencedorAsync(idempotencia, cancellationToken).ConfigureAwait(false);
            }

            // RN011: conflito de janela do veículo. A violação direta da EXCLUDE chega
            // como 23P01; sob concorrência a transação perdedora pode falhar de outras
            // formas. Reconfirmamos relendo a agenda do veículo — havendo agendamento
            // conflitante persistido, o desfecho é 409, não 500.
            if (await FalhaIndicaConflitoVeiculoAsync(ex, agendamento, cancellationToken).ConfigureAwait(false))
            {
                throw new AgendamentoConflitanteException(
                    AgendamentoConflitanteException.MensagemConfirmacao,
                    ex);
            }

            throw;
        }
    }

    /// <summary>
    /// Após perder a corrida de idempotência, relê o registro vencedor (fora da
    /// transação revertida) e decide replay (mesmo payload) ou conflito.
    /// </summary>
    private async Task<ResultadoConfirmacaoIdempotente> ResolverVencedorAsync(
        IdempotenciaRequisicao perdedor,
        CancellationToken cancellationToken)
    {
        var vencedor = await _db.IdempotenciaRequisicoes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.IdempotencyKey == perdedor.IdempotencyKey && r.Escopo == perdedor.Escopo,
                cancellationToken)
            .ConfigureAwait(false);

        // Improvável (UNIQUE disparou mas o registro sumiu) — tratamos como conflito.
        if (vencedor is null)
        {
            throw new IdempotenciaConflitanteException();
        }

        if (string.Equals(vencedor.PayloadHash, perdedor.PayloadHash, StringComparison.Ordinal))
        {
            return ResultadoConfirmacaoIdempotente.Replay(vencedor.RespostaJson);
        }

        throw new IdempotenciaConflitanteException();
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

        bool ehExclusionViolation = string.Equals(
            pg.SqlState,
            ExclusionViolationSqlState,
            StringComparison.Ordinal);

        bool ehConstraintDeVeiculo = pg.ConstraintName is { } nome
            && nome.Contains(ConstraintConflitoVeiculoPrefixo, StringComparison.OrdinalIgnoreCase);

        bool ehConcorrencia = Array.Exists(
            ConcorrenciaSqlStates,
            estado => string.Equals(pg.SqlState, estado, StringComparison.Ordinal));

        return ehExclusionViolation || ehConstraintDeVeiculo || ehConcorrencia;
    }

    /// <summary>
    /// Detecta a violação da UNIQUE <c>uq_idempotencia_key_escopo</c> (RF015):
    /// SQLSTATE <c>23505</c> (unique_violation) com a constraint da idempotência.
    /// </summary>
    private static bool IsIdempotenciaViolation(DbUpdateException exception)
    {
        if (exception.InnerException is not PostgresException pg)
        {
            return false;
        }

        bool ehUniqueViolation = string.Equals(
            pg.SqlState,
            UniqueViolationSqlState,
            StringComparison.Ordinal);

        bool ehConstraintDeIdempotencia = pg.ConstraintName is { } nome
            && nome.Contains(ConstraintIdempotenciaKeyEscopo, StringComparison.OrdinalIgnoreCase);

        return ehUniqueViolation && ehConstraintDeIdempotencia;
    }
}
