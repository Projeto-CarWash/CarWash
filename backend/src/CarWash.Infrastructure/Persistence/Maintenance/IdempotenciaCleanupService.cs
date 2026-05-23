using CarWash.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CarWash.Infrastructure.Persistence.Maintenance;

/// <summary>
/// Job de varredura diária que remove os registros de idempotência expirados
/// (RF015 / ADR 0004). Mantém a tabela <c>idempotencia_requisicoes</c> enxuta —
/// a janela de validade é de 24h e os registros não têm utilidade após expirar.
/// Usa <c>ExecuteDeleteAsync</c> (DELETE em lote, sem materializar entidades).
/// </summary>
public sealed class IdempotenciaCleanupService : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromDays(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IdempotenciaCleanupService> _logger;

    public IdempotenciaCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<IdempotenciaCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Primeira limpeza logo no startup; depois 1×/dia.
        using var timer = new PeriodicTimer(Intervalo);
        do
        {
            await LimparAsync(stoppingToken).ConfigureAwait(false);
        }
        while (!stoppingToken.IsCancellationRequested
               && await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    private async Task LimparAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<CarWashDbContext>();

            var agora = DateTime.UtcNow;
            var removidos = await db.IdempotenciaRequisicoes
                .Where(r => r.ExpiraEm < agora)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            if (removidos > 0)
            {
                _logger.LogInformation(
                    "Limpeza de idempotência concluída — {Removidos} registro(s) expirado(s) removido(s).",
                    removidos);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutdown da aplicação — encerra silenciosamente.
        }
#pragma warning disable CA1031 // Job de manutenção: uma falha não deve derrubar o host; loga e tenta no próximo ciclo.
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha na limpeza de registros de idempotência expirados.");
        }
#pragma warning restore CA1031
    }
}
