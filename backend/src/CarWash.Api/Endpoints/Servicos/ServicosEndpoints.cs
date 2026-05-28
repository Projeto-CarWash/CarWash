using CarWash.Api.Filters;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Servicos.AlterarStatus;
using CarWash.Application.Servicos.Atualizar;
using CarWash.Application.Servicos.Common;
using CarWash.Application.Servicos.Criar;
using CarWash.Application.Servicos.Listar;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ValidationException = CarWash.Application.Common.Exceptions.ValidationException;

namespace CarWash.Api.Endpoints.Servicos;

public static class ServicosEndpoints
{
    private const string MensagemPayloadInvalido = "Dados inválidos. Verifique os campos e tente novamente.";

    public static IEndpointRouteBuilder MapServicos(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var grupo = app.MapGroup("/api/v1/servicos")
            .WithTags("Servicos")
            .RequireAuthorization();

        grupo.MapPost("/", CriarAsync)
            .WithName("CriarServico")
            .Produces<CriarServicoResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict);

        grupo.MapGet("/", ListarAsync)
            .WithName("ListarServicos")
            .Produces<ListaServicosResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        grupo.MapPut("/{id:guid}", AtualizarAsync)
            .WithName("AtualizarServico")
            .Produces<ServicoResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        grupo.MapPatch("/{id:guid}/status", AlterarStatusAsync)
            .WithName("AlterarStatusServico")
            .Produces<ServicoResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<Created<CriarServicoResponse>> CriarAsync(
        [FromBody] CriarServicoRequest? request,
        [FromServices] ICommandHandler<CriarServicoCommand, CriarServicoResponse> handler,
        [FromServices] IValidator<CriarServicoCommand> validator,
        [FromServices] ILogger<CriarServicoEndpointMarker> logger,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw ValidationProblems.BodyAusente(
                "Dados do serviço inválidos. Verifique os campos e tente novamente.",
                "Corpo da requisição ausente ou malformado.");
        }

        var traceId = http.TraceIdentifier;
        var usuarioId = ObterUsuarioId(http);

        var command = new CriarServicoCommand(
            Nome: request.Nome,
            Preco: request.Preco,
            DuracaoMin: request.DuracaoMin,
            TraceId: traceId,
            UsuarioId: usuarioId);

        await ValidarAsync(validator, command, MensagemPayloadInvalido, cancellationToken).ConfigureAwait(false);

        var resposta = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Serviço cadastrado com sucesso. TraceId: {TraceId}. ServicoId: {ServicoId}. UsuarioId: {UsuarioId}",
            traceId,
            resposta.Id,
            usuarioId);

        return TypedResults.Created($"/api/v1/servicos/{resposta.Id}", resposta);
    }

    private static async Task<Ok<ListaServicosResponse>> ListarAsync(
        [FromQuery] bool? ativo,
        [FromServices] IQueryHandler<ListarServicosQuery, ListaServicosResponse> handler,
        CancellationToken cancellationToken)
    {
        var resposta = await handler.HandleAsync(
            new ListarServicosQuery(ativo),
            cancellationToken).ConfigureAwait(false);

        return TypedResults.Ok(resposta);
    }

    private static async Task<Ok<ServicoResponse>> AtualizarAsync(
        Guid id,
        [FromBody] AtualizarServicoRequest? request,
        [FromServices] ICommandHandler<AtualizarServicoCommand, ServicoResponse> handler,
        [FromServices] IValidator<AtualizarServicoCommand> validator,
        [FromServices] ILogger<AtualizarServicoEndpointMarker> logger,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw ValidationProblems.BodyAusente(
                "Dados do serviço inválidos. Verifique os campos e tente novamente.",
                "Corpo da requisição ausente ou malformado.");
        }

        var usuarioId = ObterUsuarioId(http);
        var command = new AtualizarServicoCommand(
            Id: id,
            Nome: request.Nome,
            Preco: request.Preco,
            DuracaoMin: request.DuracaoMin,
            UsuarioId: usuarioId);

        await ValidarAsync(validator, command, MensagemPayloadInvalido, cancellationToken).ConfigureAwait(false);

        var resposta = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Serviço atualizado. ServicoId: {ServicoId}. UsuarioId: {UsuarioId}", id, usuarioId);

        return TypedResults.Ok(resposta);
    }

    private static async Task<Ok<ServicoResponse>> AlterarStatusAsync(
        Guid id,
        [FromBody] AlterarStatusServicoRequest? request,
        [FromServices] ICommandHandler<AlterarStatusServicoCommand, ServicoResponse> handler,
        [FromServices] ILogger<AlterarStatusServicoEndpointMarker> logger,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        if (request is null || request.Ativo is null)
        {
            throw new ValidationException(
                MensagemPayloadInvalido,
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["ativo"] = ["Campo 'ativo' é obrigatório."],
                });
        }

        var usuarioId = ObterUsuarioId(http);
        var command = new AlterarStatusServicoCommand(id, request.Ativo.Value, usuarioId);

        var resposta = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Status do serviço alterado. ServicoId: {ServicoId}. Ativo: {Ativo}. UsuarioId: {UsuarioId}",
            id,
            request.Ativo.Value,
            usuarioId);

        return TypedResults.Ok(resposta);
    }

    private static async Task ValidarAsync<T>(
        IValidator<T> validator,
        T instancia,
        string mensagem,
        CancellationToken cancellationToken)
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

    internal sealed class CriarServicoEndpointMarker { }
    internal sealed class AtualizarServicoEndpointMarker { }
    internal sealed class AlterarStatusServicoEndpointMarker { }
}
