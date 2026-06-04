using CarWash.Application.Dashboard.Metricas.Common;

namespace CarWash.Application.Interfaces;

public interface IDashboardMetricasRepository
{
    Task<DashboardMetricasDataResponse> ConsultarAsync(
        DateTimeOffset dataInicio,
        DateTimeOffset dataFim,
        Guid? filialId,
        Guid? clienteId,
        string? status,
        CancellationToken cancellationToken);
}
