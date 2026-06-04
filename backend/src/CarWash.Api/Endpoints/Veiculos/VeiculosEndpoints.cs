using CarWash.Api.Filters;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Veiculos.AlterarStatus;
using CarWash.Application.Veiculos.Atualizar;
using CarWash.Application.Veiculos.Common;
using CarWash.Application.Veiculos.Criar;
using CarWash.Application.Veiculos.CriarBatch;
using CarWash.Application.Veiculos.Listar;
using CarWash.Application.Veiculos.ObterPorId;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using ValidationException = CarWash.Application.Common.Exceptions.ValidationException;

namespace CarWash.Api.Endpoints.Veiculos;

/// <summary>
/// Endpoints de veículos (RF005). Convenções herdadas dos demais slices:
/// <c>RequireAuthorization</c>, body validado por <see cref="ValidationFilter{T}"/>
/// e categorias de log estáveis via marker types.
/// </summary>
public static class VeiculosEndpoints
{
    private const int TamanhoPaginaMaximo = 100;
    private const string MensagemPayloadInvalido =
        "Dados do veículo inválidos. Verifique os campos e tente novamente.";

    private const string MensagemBatchInvalido =
        "Dados dos veículos inválidos. Verifique os campos e tente novamente.";

    public static IEndpointRouteBuilder MapVeiculos(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var grupo = app.MapGroup("/api/v1/clientes/{clienteId:guid}/veiculos")
            .WithTags("Veiculos")
            .RequireAuthorization();

        grupo.MapPost("/", CriarAsync)
            .WithName("CriarVeiculo")
            .Produces<VeiculoResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        grupo.MapGet("/", ListarAsync)
            .WithName("ListarVeiculos")
            .Produces<ListaVeiculosResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        grupo.MapGet("/{id:guid}", ObterPorIdAsync)
            .WithName("ObterVeiculoPorId")
            .Produces<VeiculoResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        grupo.MapPut("/{id:guid}", AtualizarAsync)
            .WithName("AtualizarVeiculo")
            .Produces<VeiculoResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        grupo.MapPatch("/{id:guid}/status", AlterarStatusAsync)
            .WithName("AlterarStatusVeiculo")
            .Produces<VeiculoResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        grupo.MapPost("/batch", CriarBatchAsync)
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

    private static async Task<Ok<ListaVeiculosResponse>> ListarAsync(
        Guid clienteId,
        [FromQuery] string? busca,
        [FromQuery] bool? ativo,
        [FromServices] IQueryHandler<ListarVeiculosQuery, ListaVeiculosResponse> handler,
        HttpContext http,
        CancellationToken cancellationToken,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20)
    {
        var erros = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        if (pagina < 1)
        {
            erros["pagina"] = ["Página deve ser maior ou igual a 1."];
        }

        if (tamanhoPagina < 1)
        {
            erros["tamanhoPagina"] = ["Tamanho da página deve ser maior ou igual a 1."];
        }
        else if (tamanhoPagina > TamanhoPaginaMaximo)
        {
            erros["tamanhoPagina"] = [$"Tamanho da página deve ser no máximo {TamanhoPaginaMaximo}."];
        }

        if (erros.Count > 0)
        {
            throw new ValidationException(
                "Parâmetros de paginação inválidos. Verifique os campos e tente novamente.",
                erros);
        }

        http.Response.Headers[HeaderNames.CacheControl] = "no-store";

        var resposta = await handler.HandleAsync(
            new ListarVeiculosQuery(clienteId, busca, ativo, pagina, tamanhoPagina),
            cancellationToken).ConfigureAwait(false);

        return TypedResults.Ok(resposta);
    }

    private static async Task<Ok<VeiculoResponse>> ObterPorIdAsync(
        Guid clienteId,
        Guid id,
        [FromServices] IQueryHandler<ObterVeiculoPorIdQuery, VeiculoResponse> handler,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        http.Response.Headers[HeaderNames.CacheControl] = "no-store";

        var resposta = await handler.HandleAsync(
            new ObterVeiculoPorIdQuery(clienteId, id),
            cancellationToken).ConfigureAwait(false);

        return TypedResults.Ok(resposta);
    }

    private static async Task<Ok<VeiculoResponse>> AtualizarAsync(
        Guid clienteId,
        Guid id,
        [FromBody] AtualizarVeiculoRequest? request,
        [FromServices] ICommandHandler<AtualizarVeiculoCommand, VeiculoResponse> handler,
        [FromServices] IValidator<AtualizarVeiculoCommand> validator,
        [FromServices] ILogger<AtualizarVeiculoEndpointMarker> logger,
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

        var command = new AtualizarVeiculoCommand(
            VeiculoId: id,
            ClienteId: clienteId,
            Placa: request.Placa,
            Modelo: request.Modelo,
            Fabricante: request.Fabricante,
            Cor: request.Cor,
            Ano: request.Ano,
            TraceId: traceId,
            UsuarioId: usuarioId);

        await ValidarAsync(validator, command, MensagemPayloadInvalido, cancellationToken)
            .ConfigureAwait(false);

        var resposta = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Veículo atualizado. TraceId: {TraceId}. VeiculoId: {VeiculoId}. ClienteId: {ClienteId}. UsuarioId: {UsuarioId}",
            traceId,
            id,
            clienteId,
            usuarioId);

        return TypedResults.Ok(resposta);
    }

    private static async Task<Ok<VeiculoResponse>> AlterarStatusAsync(
        Guid clienteId,
        Guid id,
        [FromBody] AlterarStatusVeiculoRequest? request,
        [FromServices] ICommandHandler<AlterarStatusVeiculoCommand, VeiculoResponse> handler,
        [FromServices] IValidator<AlterarStatusVeiculoCommand> validator,
        [FromServices] ILogger<AlterarStatusVeiculoEndpointMarker> logger,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Ativo is null)
        {
            throw new ValidationException(
                MensagemPayloadInvalido,
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["ativo"] = ["Campo 'ativo' é obrigatório."],
                });
        }

        var traceId = http.TraceIdentifier;
        var usuarioId = ObterUsuarioId(http);

        var command = new AlterarStatusVeiculoCommand(
            ClienteId: clienteId,
            VeiculoId: id,
            Ativo: request.Ativo,
            TraceId: traceId,
            UsuarioId: usuarioId);

        await ValidarAsync(validator, command, MensagemPayloadInvalido, cancellationToken)
            .ConfigureAwait(false);

        var resposta = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Status do veículo alterado. TraceId: {TraceId}. VeiculoId: {VeiculoId}. ClienteId: {ClienteId}. Ativo: {Ativo}. UsuarioId: {UsuarioId}",
            traceId,
            id,
            clienteId,
            request.Ativo.Value,
            usuarioId);

        return TypedResults.Ok(resposta);
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
            Placa: v.Placa, Modelo: v.Modelo, Fabricante: v.Fabricante, Cor: v.Cor, Ano: v.Ano)).ToList();

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

    private static async Task ValidarAsync<T>(
        IValidator<T> validator, T instancia, string mensagem, CancellationToken cancellationToken)
    {
        var resultado = await validator.ValidateAsync(instancia, cancellationToken).ConfigureAwait(false);
        ValidationProblems.EnsureValid(resultado, mensagem);
    }

    private static Guid? ObterUsuarioId(HttpContext http)
    {
        var sub = http.User.FindFirst("sub")?.Value
            ?? http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

#pragma warning disable S2094, SA1502, CA1812 // Marcadores apenas para tipar o ILogger por endpoint (categoria de log estável).

    /// <summary>Marker usado em <c>ILogger&lt;T&gt;</c> para categoria de log estável.</summary>
    internal sealed class CriarVeiculoEndpointMarker
    {
    }

    /// <summary>Marker usado em <c>ILogger&lt;T&gt;</c> para categoria de log estável.</summary>
    internal sealed class AtualizarVeiculoEndpointMarker
    {
    }

    /// <summary>Marker usado em <c>ILogger&lt;T&gt;</c> para categoria de log estável.</summary>
    internal sealed class AlterarStatusVeiculoEndpointMarker
    {
    }

#pragma warning restore S2094, SA1502, CA1812
}
