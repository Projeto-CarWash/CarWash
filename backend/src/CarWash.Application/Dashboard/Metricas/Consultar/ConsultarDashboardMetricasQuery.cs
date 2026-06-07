using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Dashboard.Metricas.Common;

namespace CarWash.Application.Dashboard.Metricas.Consultar;

public sealed record ConsultarDashboardMetricasQuery(
    DateTimeOffset DataInicio,
    DateTimeOffset DataFim,
    Guid? FilialId,
    Guid? ClienteId,
    string? Status,
    string TraceId,
    Guid? UsuarioId) : IQuery<DashboardMetricasResponse>;
