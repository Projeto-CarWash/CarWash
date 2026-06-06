using System.Security.Claims;
using CarWash.Api.Middleware;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Usuarios.Preferencias.AtualizarTema;
using CarWash.Application.Usuarios.Preferencias.Common;
using CarWash.Application.Usuarios.Preferencias.Consultar;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace CarWash.Api.Endpoints.Usuarios;

public static class UsuarioPreferenciasEndpoints
{
    public static IEndpointRouteBuilder MapUsuarioPreferencias(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var grupo = app.MapGroup("/api/v1/usuarios/me/preferencias")
            .WithTags("Usuarios - Preferências")
            .RequireAuthorization();

        grupo.MapGet("/", ConsultarAsync)
            .WithName("ConsultarMinhasPreferencias")
            .Produces<UsuarioPreferenciasResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        grupo.MapPatch("/tema", AtualizarTemaAsync)
            .WithName("AtualizarMinhaPreferenciaTema")
            .Produces<UsuarioPreferenciasResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> ConsultarAsync(
        [FromServices] IQueryHandler<ConsultarMinhasPreferenciasQuery, UsuarioPreferenciasResponse> handler,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId(http);

        if (!usuarioId.HasValue)
        {
            return Results.Unauthorized();
        }

        string traceId = ObterTraceId(http);

        var response = await handler
            .HandleAsync(
                new ConsultarMinhasPreferenciasQuery(usuarioId.Value, traceId),
                cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(response);
    }

    private static async Task<IResult> AtualizarTemaAsync(
        [FromBody] AtualizarTemaRequest? request,
        [FromServices] ICommandHandler<AtualizarTemaUsuarioCommand, UsuarioPreferenciasResponse> handler,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId(http);

        if (!usuarioId.HasValue)
        {
            return Results.Unauthorized();
        }

        string traceId = ObterTraceId(http);

        var command = new AtualizarTemaUsuarioCommand(
            usuarioId.Value,
            request?.Theme,
            traceId);

        try
        {
            var response = await handler
                .HandleAsync(command, cancellationToken)
                .ConfigureAwait(false);

            return Results.Ok(response);
        }
        catch (ValidationException ex)
        {
            return Results.BadRequest(new
            {
                code = "THEME_VALIDATION_ERROR",
                message = "Tema inválido. Informe light ou dark.",
                traceId,
                details = ex.Errors.Select(error => new
                {
                    field = error.PropertyName,
                    message = error.ErrorMessage,
                }),
            });
        }
    }

    private static string ObterTraceId(HttpContext http)
    {
        return http.Items[CorrelationIdMiddleware.ItemKey] as string
            ?? http.TraceIdentifier;
    }

    private static Guid? ObterUsuarioId(HttpContext http)
    {
        string? raw = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? http.User.FindFirst("sub")?.Value
            ?? http.User.FindFirst("usuario_id")?.Value
            ?? http.User.FindFirst("uid")?.Value;

        return Guid.TryParse(raw, out var usuarioId)
            ? usuarioId
            : null;
    }
}
