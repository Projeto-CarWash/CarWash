using System.Security.Claims;
using CarWash.Api.Middleware;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Dashboard.Metricas.Common;
using CarWash.Application.Dashboard.Metricas.Consultar;
using FluentValidation;

namespace CarWash.Api.Endpoints.Dashboard;

public static class DashboardMetricasEndpoints
{
    public static IEndpointRouteBuilder MapDashboardMetricas(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/dashboard")
            .WithTags("Dashboard")
            .RequireAuthorization();

        group.MapGet("/metricas", ConsultarMetricas)
            .WithName("ConsultarDashboardMetricas")
            .Produces<DashboardMetricasResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> ConsultarMetricas(
        DateTimeOffset? dataInicio,
        DateTimeOffset? dataFim,
        string? filialId,
        string? clienteId,
        string? status,
        IQueryHandler<ConsultarDashboardMetricasQuery, DashboardMetricasResponse> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        string traceId = ObterTraceId(httpContext);
        var usuarioId = ObterUsuarioId(httpContext);

        var details = new List<object>();

        if (!dataInicio.HasValue)
        {
            details.Add(new
            {
                field = "dataInicio",
                message = "dataInicio é obrigatório.",
            });
        }

        if (!dataFim.HasValue)
        {
            details.Add(new
            {
                field = "dataFim",
                message = "dataFim é obrigatório.",
            });
        }

        Guid? filialGuid = null;
        if (!string.IsNullOrWhiteSpace(filialId))
        {
            if (!Guid.TryParse(filialId, out var parsedFilialId))
            {
                details.Add(new
                {
                    field = "filialId",
                    message = "filialId deve ser um UUID válido.",
                });
            }
            else
            {
                filialGuid = parsedFilialId;
            }
        }

        Guid? clienteGuid = null;
        if (!string.IsNullOrWhiteSpace(clienteId))
        {
            if (!Guid.TryParse(clienteId, out var parsedClienteId))
            {
                details.Add(new
                {
                    field = "clienteId",
                    message = "clienteId deve ser um UUID válido.",
                });
            }
            else
            {
                clienteGuid = parsedClienteId;
            }
        }

        if (details.Count > 0)
        {
            return Results.BadRequest(new
            {
                code = "DASHBOARD_VALIDATION_ERROR",
                message = "Parâmetros de consulta inválidos. Verifique os filtros e tente novamente.",
                traceId,
                details,
            });
        }

        var query = new ConsultarDashboardMetricasQuery(
            DataInicio: dataInicio!.Value,
            DataFim: dataFim!.Value,
            FilialId: filialGuid,
            ClienteId: clienteGuid,
            Status: status,
            TraceId: traceId,
            UsuarioId: usuarioId);

        try
        {
            var response =
                await handler.HandleAsync(query, cancellationToken);

            return Results.Ok(response);
        }
        catch (ValidationException ex)
        {
            return Results.BadRequest(new
            {
                code = "DASHBOARD_VALIDATION_ERROR",
                message = "Parâmetros de consulta inválidos. Verifique os filtros e tente novamente.",
                traceId,
                details = ex.Errors.Select(e => new
                {
                    field = e.PropertyName,
                    message = e.ErrorMessage,
                }),
            });
        }
    }

    private static string ObterTraceId(HttpContext httpContext)
    {
        return httpContext.Items[CorrelationIdMiddleware.ItemKey] as string
            ?? httpContext.TraceIdentifier;
    }

    private static Guid? ObterUsuarioId(HttpContext httpContext)
    {
        string? raw = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? httpContext.User.FindFirst("sub")?.Value;

        return Guid.TryParse(raw, out var usuarioId)
            ? usuarioId
            : null;
    }
}
