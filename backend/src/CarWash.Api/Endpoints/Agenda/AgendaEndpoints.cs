using System.Diagnostics;
using CarWash.Api.Filters;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Agenda.Common;
using CarWash.Application.Agenda.Consultar;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace CarWash.Api.Endpoints.Agenda;

/// <summary>
/// Endpoint de consulta de agenda (RF009): <c>GET /api/v1/agenda</c> somente-leitura,
/// com formatos simples/detalhado. Sempre envia <c>Cache-Control: no-store</c> —
/// ambos os formatos carregam PII (ADR 0004 — L4).
/// </summary>
public static class AgendaEndpoints
{
    private const string MensagemParametrosInvalidos =
        "Parâmetros de consulta inválidos. Verifique período, formato e filtros informados.";

    public static IEndpointRouteBuilder MapAgenda(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var grupo = app.MapGroup("/api/v1/agenda")
            .WithTags("Agenda")
            .RequireAuthorization();

        grupo.MapGet("/", ConsultarAsync)
            .WithName("ConsultarAgenda")
            .Produces<ConsultarAgendaResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<Ok<ConsultarAgendaResponse>> ConsultarAsync(
        [FromQuery] string? formato,
        [FromQuery] string? inicio,
        [FromQuery] string? fim,
        [FromQuery] string? filialId,
        [FromQuery] string? clienteId,
        [FromQuery] string? usuarioId,
        [FromQuery] string? status,
        [FromServices] IQueryHandler<ConsultarAgendaQuery, ConsultarAgendaResponse> handler,
        [FromServices] IValidator<ConsultarAgendaQuery> validator,
        [FromServices] ILogger<AgendaEndpointMarker> logger,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var query = new ConsultarAgendaQuery(
            Formato: formato,
            Inicio: inicio,
            Fim: fim,
            FilialId: filialId,
            ClienteId: clienteId,
            UsuarioId: usuarioId,
            Status: status,
            TraceId: http.TraceIdentifier);

        var resultado = await validator.ValidateAsync(query, cancellationToken).ConfigureAwait(false);
        ValidationProblems.EnsureValid(resultado, MensagemParametrosInvalidos);

        // L4: a agenda expõe PII nos dois formatos — proibir cache em proxies/browser.
        http.Response.Headers[HeaderNames.CacheControl] = "no-store";

        var cronometro = Stopwatch.StartNew();
        var resposta = await handler.HandleAsync(query, cancellationToken).ConfigureAwait(false);
        cronometro.Stop();

        // Log estruturado SEM PII: nunca registra nome/documento/placa/telefone.
        logger.LogInformation(
            "Agenda consultada. TraceId: {TraceId}. Formato: {Formato}. FilialId: {FilialId}. "
            + "Inicio: {Inicio}. Fim: {Fim}. ClienteId: {ClienteId}. UsuarioId: {UsuarioId}. "
            + "Status: {Status}. TotalItens: {TotalItens}. TempoMs: {TempoMs}",
            query.TraceId,
            formato,
            filialId,
            inicio,
            fim,
            clienteId,
            usuarioId,
            status,
            resposta.Data.Count,
            cronometro.ElapsedMilliseconds);

        return TypedResults.Ok(resposta);
    }

#pragma warning disable S2094, SA1502, CA1812 // Marcador apenas para tipar o ILogger por endpoint (categoria de log estável).

    /// <summary>Marker usado em <c>ILogger&lt;T&gt;</c> para categoria de log estável.</summary>
    internal sealed class AgendaEndpointMarker
    {
    }

#pragma warning restore S2094, SA1502, CA1812
}
