using System.Text.Json;
using CarWash.Application.Interfaces;
using CarWash.Domain.Entities;
using CarWash.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CarWash.Infrastructure.Repositories;

public sealed class AgendamentoObservacaoRepository : IAgendamentoObservacaoRepository
{
    private readonly CarWashDbContext context;

    public AgendamentoObservacaoRepository(CarWashDbContext context)
    {
        this.context = context;
    }

    public Task<bool> AgendamentoExisteAsync(
        Guid agendamentoId,
        CancellationToken cancellationToken)
    {
        return context.Set<Agendamento>()
            .AsNoTracking()
            .AnyAsync(x => x.Id == agendamentoId, cancellationToken);
    }

    public Task<AgendamentoObservacao?> ObterPorIdEAgendamentoAsync(
        Guid observacaoId,
        Guid agendamentoId,
        CancellationToken cancellationToken)
    {
        return context.AgendamentoObservacoes
            .FirstOrDefaultAsync(
                x => x.Id == observacaoId && x.AgendamentoId == agendamentoId,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<AgendamentoObservacao>> ListarPorAgendamentoAsync(
        Guid agendamentoId,
        bool incluirInativas,
        CancellationToken cancellationToken)
    {
        IQueryable<AgendamentoObservacao> query = context.AgendamentoObservacoes
            .AsNoTracking()
            .Where(x => x.AgendamentoId == agendamentoId);

        if (!incluirInativas)
        {
            query = query.Where(x => x.Ativo);
        }

        return await query
            .OrderByDescending(x => x.CriadoEm)
            .ToListAsync(cancellationToken);
    }

    public async Task AdicionarAsync(
        AgendamentoObservacao observacao,
        string traceId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        await context.AgendamentoObservacoes.AddAsync(observacao, cancellationToken);

        await context.AuditLogs.AddAsync(
            AuditLog.Registrar(
                id: Guid.NewGuid(),
                evento: "OBSERVACAO_AGENDAMENTO_CRIADA",
                entidade: "agendamento_observacoes",
                correlationId: traceId,
                entidadeId: observacao.Id,
                usuarioId: observacao.CriadoPor,
                dados: JsonSerializer.Serialize(new
                {
                    traceId,
                    observacao.AgendamentoId,
                    ObservacaoId = observacao.Id,
                    UsuarioId = observacao.CriadoPor,
                    Timestamp = observacao.CriadoEm,
                    Antes = (object?)null,
                    Depois = new
                    {
                        observacao.Texto,
                        observacao.Ativo,
                    },
                    Origem = "api",
                })),
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task AtualizarAsync(
        AgendamentoObservacao observacao,
        string textoAnterior,
        string traceId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        await context.AuditLogs.AddAsync(
            AuditLog.Registrar(
                id: Guid.NewGuid(),
                evento: "OBSERVACAO_AGENDAMENTO_ATUALIZADA",
                entidade: "agendamento_observacoes",
                correlationId: traceId,
                entidadeId: observacao.Id,
                usuarioId: observacao.AtualizadoPor,
                dados: JsonSerializer.Serialize(new
                {
                    traceId,
                    observacao.AgendamentoId,
                    ObservacaoId = observacao.Id,
                    UsuarioId = observacao.AtualizadoPor,
                    Timestamp = observacao.AtualizadoEm,
                    Antes = new
                    {
                        Texto = textoAnterior,
                    },
                    Depois = new
                    {
                        observacao.Texto,
                    },
                    Origem = "api",
                })),
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task ExcluirAsync(
        AgendamentoObservacao observacao,
        string textoAnterior,
        string traceId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        await context.AuditLogs.AddAsync(
            AuditLog.Registrar(
                id: Guid.NewGuid(),
                evento: "OBSERVACAO_AGENDAMENTO_EXCLUIDA",
                entidade: "agendamento_observacoes",
                correlationId: traceId,
                entidadeId: observacao.Id,
                usuarioId: observacao.ExcluidoPor,
                dados: JsonSerializer.Serialize(new
                {
                    traceId,
                    observacao.AgendamentoId,
                    ObservacaoId = observacao.Id,
                    UsuarioId = observacao.ExcluidoPor,
                    Timestamp = observacao.ExcluidoEm,
                    Antes = new
                    {
                        Texto = textoAnterior,
                        Ativo = true,
                    },
                    Depois = new
                    {
                        observacao.Ativo,
                        observacao.ExcluidoEm,
                    },
                    Origem = "api",
                })),
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
