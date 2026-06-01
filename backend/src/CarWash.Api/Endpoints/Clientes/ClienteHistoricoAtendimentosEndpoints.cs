using CarWash.Api.Middleware;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Clientes.HistoricoAtendimentos.Common;
using CarWash.Application.Clientes.HistoricoAtendimentos.Consultar;
using FluentValidation;

namespace CarWash.Api.Endpoints.Clientes;

public static class ClienteHistoricoAtendimentosEndpoints
{
    public static IEndpointRouteBuilder MapClienteHistoricoAtendimentos(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup("/api/v1/clientes")
            .WithTags("Clientes")
            .RequireAuthorization();

        group.MapGet("/{clienteId}/historico-atendimentos", ConsultarHistorico)
            .WithName("ConsultarHistoricoAtendimentosCliente")
            .Produces<HistoricoAtendimentosResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> ConsultarHistorico(
        string clienteId,
        DateTimeOffset? dataInicio,
        DateTimeOffset? dataFim,
        int? ultimosDias,
        string? status,
        int? page,
        int? pageSize,
        IQueryHandler<ConsultarHistoricoAtendimentosClienteQuery, HistoricoAtendimentosResponse> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        string traceId = ObterTraceId(httpContext);

        if (!Guid.TryParse(clienteId, out Guid clienteGuid))
        {
            return Results.BadRequest(new
            {
                code = "HISTORICO_ATENDIMENTOS_VALIDATION_ERROR",
                message = "Parâmetros de consulta inválidos. Verifique os filtros e tente novamente.",
                traceId,
                details = new[]
                {
                    new
                    {
                        field = "clienteId",
                        message = "ClienteId deve ser um UUID válido.",
                    },
                },
            });
        }

        var query = new ConsultarHistoricoAtendimentosClienteQuery(
            ClienteId: clienteGuid,
            DataInicio: dataInicio,
            DataFim: dataFim,
            UltimosDias: ultimosDias,
            Status: status,
            Page: page ?? 1,
            PageSize: pageSize ?? 20,
            TraceId: traceId);

        try
        {
            HistoricoAtendimentosResponse response =
                await handler.HandleAsync(query, cancellationToken);

            return Results.Ok(response);
        }
        catch (ValidationException ex)
        {
            return Results.BadRequest(new
            {
                code = "HISTORICO_ATENDIMENTOS_VALIDATION_ERROR",
                message = "Parâmetros de consulta inválidos. Verifique os filtros e tente novamente.",
                traceId,
                details = ex.Errors.Select(e => new
                {
                    field = e.PropertyName,
                    message = e.ErrorMessage,
                }),
            });
        }
        catch (ClienteHistoricoNaoEncontradoException)
        {
            return Results.NotFound(new
            {
                code = "CLIENTE_NOT_FOUND",
                message = "Cliente não encontrado para consulta de histórico.",
                traceId,
                details = Array.Empty<object>(),
            });
        }
    }

    private static string ObterTraceId(HttpContext httpContext)
    {
        return httpContext.Items[CorrelationIdMiddleware.ItemKey] as string
            ?? httpContext.TraceIdentifier;
    }
}
