using CarWash.Application.Agendamentos.Common;
using CarWash.Application.Agendamentos.Persistence;
using CarWash.Domain.Entities;
using CarWash.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CarWash.Infrastructure.Persistence.Repositories;

public sealed class AgendamentoRepository : IAgendamentoRepository
{
    private readonly CarWashDbContext _db;
    private readonly ILogger<AgendamentoRepository> _logger;

    public AgendamentoRepository(
        CarWashDbContext db,
        ILogger<AgendamentoRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<int> ContarOcupacaoAsync(
        Guid filialId,
        DateTime inicio,
        DateTime fim,
        CancellationToken cancellationToken)
    {
        var statusOcupacao = new[] { "agendado", "em_andamento" };
        return await _db.Agendamentos
            .Where(a => a.FilialId == filialId
                && a.Inicio < fim
                && a.Fim > inicio
                && statusOcupacao.Contains(a.StatusRaw))
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> ExisteConflitoVeiculoAsync(
        Guid veiculoId,
        DateTime inicio,
        DateTime fim,
        CancellationToken cancellationToken)
    {
        var statusOcupacao = new[] { "agendado", "em_andamento" };
        return await _db.Agendamentos
            .AnyAsync(
                a => a.VeiculoId == veiculoId
                    && a.Inicio < fim
                    && a.Fim > inicio
                    && statusOcupacao.Contains(a.StatusRaw),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Filial?> ObterFilialPorIdAsync(
        Guid filialId,
        CancellationToken cancellationToken)
    {
        return await _db.Filiais
            .FirstOrDefaultAsync(f => f.Id == filialId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Cliente?> ObterClientePorIdAsync(
        Guid clienteId,
        CancellationToken cancellationToken)
    {
        return await _db.Clientes
            .FirstOrDefaultAsync(c => c.Id == clienteId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Veiculo?> ObterVeiculoPorIdAsync(
        Guid veiculoId,
        CancellationToken cancellationToken)
    {
        return await _db.Veiculos
            .FirstOrDefaultAsync(v => v.Id == veiculoId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Filiado?> ObterFiliadoPorIdAsync(
        Guid filiadoId,
        CancellationToken cancellationToken)
    {
        return await _db.Filiados
            .FirstOrDefaultAsync(f => f.Id == filiadoId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Servico>> ObterServicosPorIdsAsync(
        IReadOnlyList<Guid> servicoIds,
        CancellationToken cancellationToken)
    {
        return await _db.Servicos
            .Where(s => servicoIds.Contains(s.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task CriarAsync(
        Agendamento agendamento,
        List<AgendamentoItem> itens,
        AgendamentoHistorico historico,
        string traceId,
        Guid? usuarioId,
        CancellationToken cancellationToken)
    {
        await using var tx = await _db.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var filial = await _db.Filiais.AsNoTracking()
                .FirstAsync(f => f.Id == agendamento.FilialId, cancellationToken)
                .ConfigureAwait(false);

            var ocupacao = await ContarOcupacaoAsync(
                agendamento.FilialId,
                agendamento.Inicio,
                agendamento.Fim,
                cancellationToken).ConfigureAwait(false);

            if (ocupacao >= filial.CelulasAtivas)
            {
                throw new CapacidadeFilialAtingidaException();
            }

            _db.Agendamentos.Add(agendamento);
            _db.AgendamentoItens.AddRange(itens);
            _db.AgendamentoHistoricos.Add(historico);

            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException pg
                && pg.ConstraintName == "ex_ag_veiculo_janela")
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogWarning(
                ex,
                "EXCLUDE constraint violada (concorrencia). TraceId: {TraceId}",
                traceId);
            throw new VeiculoConflitoException(ex);
        }
        catch (CapacidadeFilialAtingidaException)
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }
}
