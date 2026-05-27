using CarWash.Api.Filters;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Filiais.Criar;
using CarWash.Application.Filiais.Listar;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ValidationException = CarWash.Application.Common.Exceptions.ValidationException;

namespace CarWash.Api.Endpoints.Filiais;

/// <summary>
/// Endpoints de cadastro e listagem de filiais (RF017 + RF018). Aplicam o
/// contrato HTTP do ADR-0007 §4: envelope {id, mensagem, traceId} no POST,
/// listagem paginada compatível com <c>frontend/src/types/filial.ts</c>. Grupo
/// inteiro requer autenticação (L5=a — RequireAuthorization() puro).
/// </summary>
public static class FiliaisEndpoints
{
    private const int TamanhoPaginaMaximo = 100;
    private const string MensagemPayloadInvalido =
        "Dados da filial inválidos. Verifique os campos e tente novamente.";

    public static IEndpointRouteBuilder MapFiliais(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var grupo = app.MapGroup("/api/v1/filiais")
            .WithTags("Filiais")
            .RequireAuthorization();

        grupo.MapPost("/", CriarAsync)
            .WithName("CriarFilial")
            .Produces<CriarFilialResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        grupo.MapGet("/", ListarAsync)
            .WithName("ListarFiliais")
            .Produces<ListaFiliaisResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<Created<CriarFilialResponse>> CriarAsync(
        [FromBody] CriarFilialRequest? request,
        [FromServices] ICommandHandler<CriarFilialCommand, CriarFilialResponse> handler,
        [FromServices] IValidator<CriarFilialCommand> validator,
        [FromServices] ILogger<CriarFilialEndpointMarker> logger,
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

        var command = new CriarFilialCommand(
            Nome: request.Nome,
            Codigo: request.Codigo,
            Cnpj: request.Cnpj,
            CelulasAtivas: request.CelulasAtivas,
            Timezone: request.Timezone,
            Endereco: request.Endereco,
            TraceId: traceId,
            UsuarioId: usuarioId);

        await ValidarAsync(validator, command, MensagemPayloadInvalido, cancellationToken).ConfigureAwait(false);

        var resposta = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        // Log estruturado — TraceId, FilialId, Codigo, UsuarioId. CNPJ
        // intencionalmente fora dos logs (PII fiscal — ADR-0007 §6.4).
        logger.LogInformation(
            "Filial cadastrada. TraceId: {TraceId}. FilialId: {FilialId}. Codigo: {Codigo}. UsuarioId: {UsuarioId}",
            traceId,
            resposta.Id,
            command.Codigo,
            usuarioId);

        return TypedResults.Created($"/api/v1/filiais/{resposta.Id}", resposta);
    }

    private static async Task<Ok<ListaFiliaisResponse>> ListarAsync(
        [FromQuery] string? busca,
        [FromQuery] bool? ativo,
        [FromServices] IQueryHandler<ListarFiliaisQuery, ListaFiliaisResponse> handler,
        CancellationToken cancellationToken,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20)
    {
        // Validação explícita de paginação (mesmo padrão de ClientesEndpoints).
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

        var resposta = await handler.HandleAsync(
            new ListarFiliaisQuery(busca, ativo, pagina, tamanhoPagina),
            cancellationToken).ConfigureAwait(false);

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

#pragma warning disable S2094, SA1502, CA1812 // Marcador para tipar ILogger por endpoint (categoria de log estável).

    /// <summary>Marker usado em <c>ILogger&lt;T&gt;</c> para categoria de log estável.</summary>
    internal sealed class CriarFilialEndpointMarker
    {
    }

#pragma warning restore S2094, SA1502, CA1812
}
