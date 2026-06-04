using System.Diagnostics;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Dashboard.Metricas.Common;
using CarWash.Application.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace CarWash.Application.Dashboard.Metricas.Consultar;

public sealed class ConsultarDashboardMetricasHandler
    : IQueryHandler<ConsultarDashboardMetricasQuery, DashboardMetricasResponse>
{
    private readonly IDashboardMetricasRepository repository;
    private readonly IValidator<ConsultarDashboardMetricasQuery> validator;
    private readonly ILogger<ConsultarDashboardMetricasHandler> logger;

    public ConsultarDashboardMetricasHandler(
        IDashboardMetricasRepository repository,
        IValidator<ConsultarDashboardMetricasQuery> validator,
        ILogger<ConsultarDashboardMetricasHandler> logger)
    {
        this.repository = repository;
        this.validator = validator;
        this.logger = logger;
    }

    public async Task<DashboardMetricasResponse> HandleAsync(
        ConsultarDashboardMetricasQuery query,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        DashboardMetricasDataResponse data = await repository.ConsultarAsync(
            query.DataInicio,
            query.DataFim,
            query.FilialId,
            query.ClienteId,
            query.Status,
            cancellationToken);

        stopwatch.Stop();

        int quantidadeRegistrosAgregados = data.Operacional.TotalAtendimentos;

        logger.LogInformation(
            "Dashboard consultado. TraceId: {TraceId}. UsuarioId: {UsuarioId}. DataInicio: {DataInicio}. DataFim: {DataFim}. FilialId: {FilialId}. ClienteId: {ClienteId}. Status: {Status}. QuantidadeRegistrosAgregados: {QuantidadeRegistrosAgregados}. TempoMs: {TempoMs}",
            query.TraceId,
            query.UsuarioId,
            query.DataInicio,
            query.DataFim,
            query.FilialId,
            query.ClienteId,
            query.Status,
            quantidadeRegistrosAgregados,
            stopwatch.ElapsedMilliseconds);

        return new DashboardMetricasResponse
        {
            Message = quantidadeRegistrosAgregados == 0
                ? "Nenhum dado encontrado para o período selecionado."
                : "Métricas do dashboard consultadas com sucesso.",
            Data = data,
            TraceId = query.TraceId,
        };
    }
}
