using CarWash.Api.Filters;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Veiculos.Common;
using CarWash.Application.Veiculos.Criar;
using CarWash.Application.Veiculos.CriarBatch;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace CarWash.Api.Endpoints.Veiculos;

/// <summary>
/// Endpoints de veículos (RF005). Convenções herdadas dos demais slices:
/// <c>RequireAuthorization</c>, body validado por <see cref="ValidationFilter{T}"/>
/// e categorias de log estáveis via marker types.
/// </summary>
public static class VeiculosEndpoints
{
    private const string MensagemPayloadInvalido =
        "Dados do veículo inválidos. Verifique os campos e tente novamente.";

    private const string MensagemBatchInvalido =
        "Dados dos veículos inválidos. Verifique os campos e tente novamente.";

    public static IEndpointRouteBuilder MapVeiculos(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/v1/clientes/{clienteId:guid}/veiculos", CriarAsync)
            .RequireAuthorization()
            .WithTags("Veiculos")
            .WithName("CriarVeiculo")
            .Produces<VeiculoResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        app.MapPost("/api/v1/clientes/{clienteId:guid}/veiculos/batch", CriarBatchAsync)
            .RequireAuthorization()
            .WithTags("Veiculos")
            .WithName("CriarVeiculosBatch")
            .Produces<IReadOnlyList<VeiculoResponse>>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return app;
    }

    private static async Task<Created<VeiculoResponse>> CriarAsync(
        Guid clienteId,
        [FromBody] CriarVeiculoRequest? request,
        [FromServices] ICommandHandler<CriarVeiculoCommand, VeiculoResponse> handler,
        [FromServices] IValidator<CriarVeiculoCommand> validator,
        [FromServices] ILogger<CriarVeiculoEndpointMarker> logger,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw ValidationProblems.BodyAusente(
                MensagemPayloadInvalido,
                "Corpo da requisição ausente ou malformado.");
        }

        var traceId = http.TraceIdentifier;
        var usuarioId = ObterUsuarioId(http);

        var command = new CriarVeiculoCommand(
            ClienteId: clienteId,
            Placa: request.Placa,
            Modelo: request.Modelo,
            Fabricante: request.Fabricante,
            Cor: request.Cor,
            Ano: request.Ano,
            TraceId: traceId,
            UsuarioId: usuarioId);

        var resultado = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        ValidationProblems.EnsureValid(resultado, MensagemPayloadInvalido);

        var resposta = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Veículo cadastrado com sucesso. TraceId: {TraceId}. VeiculoId: {VeiculoId}. ClienteId: {ClienteId}. UsuarioId: {UsuarioId}",
            traceId,
            resposta.Id,
            clienteId,
            usuarioId);

        return TypedResults.Created($"/api/v1/clientes/{clienteId}/veiculos/{resposta.Id}", resposta);
    }

    private static async Task<Created<IReadOnlyList<VeiculoResponse>>> CriarBatchAsync(
        Guid clienteId,
        [FromBody] CriarVeiculosBatchRequest? request,
        [FromServices] ICommandHandler<CriarVeiculosBatchCommand, IReadOnlyList<VeiculoResponse>> handler,
        [FromServices] IValidator<CriarVeiculosBatchCommand> validator,
        [FromServices] ILogger<CriarVeiculoEndpointMarker> logger,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw ValidationProblems.BodyAusente(
                MensagemBatchInvalido,
                "Corpo da requisição ausente ou malformado.");
        }

        var traceId = http.TraceIdentifier;
        var usuarioId = ObterUsuarioId(http);

        var itens = request.Veiculos.Select(v => new VeiculoItemCommand(
            Placa: v.Placa,
            Modelo: v.Modelo,
            Fabricante: v.Fabricante,
            Cor: v.Cor,
            Ano: v.Ano)).ToList();

        var command = new CriarVeiculosBatchCommand(
            ClienteId: clienteId,
            Veiculos: itens,
            TraceId: traceId,
            UsuarioId: usuarioId);

        var resultado = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        ValidationProblems.EnsureValid(resultado, MensagemBatchInvalido);

        var resposta = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Batch de veículos cadastrado com sucesso. TraceId: {TraceId}. QtdVeiculos: {Qtd}. ClienteId: {ClienteId}. UsuarioId: {UsuarioId}",
            traceId,
            resposta.Count,
            clienteId,
            usuarioId);

        return TypedResults.Created($"/api/v1/clientes/{clienteId}/veiculos/batch", resposta);
    }

    private static Guid? ObterUsuarioId(HttpContext http)
    {
        var sub = http.User.FindFirst("sub")?.Value
            ?? http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

#pragma warning disable S2094, SA1502, CA1812 // Marker para tipar o ILogger por endpoint (categoria estável).

    /// <summary>Marker usado em <c>ILogger&lt;T&gt;</c> para categoria de log estável.</summary>
    internal sealed class CriarVeiculoEndpointMarker
    {
    }

#pragma warning restore S2094, SA1502, CA1812
}
