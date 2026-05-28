using CarWash.Api.Extensions;
using CarWash.Api.Filters;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Filiais.AlterarCelulasAtivas;
using CarWash.Application.Filiais.Common;
using CarWash.Application.Filiais.CriarFilial;
using CarWash.Application.Filiais.ObterFilialPorId;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace CarWash.Api.Endpoints.Filiais;

/// <summary>
/// Endpoints REST de filiais (RF017/RF018). POST e PATCH exigem policy "Admin"
/// (decisão Q1 do refinamento). GET é apenas autenticado.
/// </summary>
public static class FiliaisEndpoints
{
    private const string MensagemPayloadInvalido =
        "Dados da filial inválidos. Verifique os campos e tente novamente.";

    public static IEndpointRouteBuilder MapFiliais(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var grupo = app.MapGroup("/api/v1/filiais")
            .WithTags("Filiais")
            .RequireAuthorization(); // autenticação base — GET cai aqui.

        // POST /api/v1/filiais — Admin.
        grupo.MapPost("/", CriarAsync)
            .RequireAuthorization(AuthorizationPoliciesExtensions.AdminPolicy)
            .AddEndpointFilter<ValidationFilter<CriarFilialCommand>>()
            .WithName("CriarFilial")
            .Produces<FilialResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        // GET /api/v1/filiais/{id} — autenticado simples.
        grupo.MapGet("/{id:guid}", ObterPorIdAsync)
            .WithName("ObterFilialPorId")
            .Produces<FilialResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // PATCH /api/v1/filiais/{id}/celulas-ativas — Admin.
        grupo.MapPatch("/{id:guid}/celulas-ativas", AlterarCelulasAtivasAsync)
            .RequireAuthorization(AuthorizationPoliciesExtensions.AdminPolicy)
            .WithName("AlterarCelulasAtivasFilial")
            .Produces<FilialResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<Created<FilialResponse>> CriarAsync(
        [FromBody] CriarFilialCommand command,
        [FromServices] ICommandHandler<CriarFilialCommand, FilialResponse> handler,
        CancellationToken cancellationToken)
    {
        var resposta = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);
        var location = $"/api/v1/filiais/{resposta.Id}";
        return TypedResults.Created(location, resposta);
    }

    private static async Task<Ok<FilialResponse>> ObterPorIdAsync(
        Guid id,
        [FromServices] IQueryHandler<ObterFilialPorIdQuery, FilialResponse> handler,
        CancellationToken cancellationToken)
    {
        var resposta = await handler.HandleAsync(new ObterFilialPorIdQuery(id), cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(resposta);
    }

    /// <summary>
    /// <c>PATCH /api/v1/filiais/{id}/celulas-ativas</c>. O <c>id</c> vem da rota;
    /// body carrega apenas <c>celulasAtivas</c>. Como o command mescla id (rota)
    /// + request (body), o <see cref="ValidationFilter{T}"/> não é aplicável e
    /// validamos inline — mesmo padrão de <c>AlterarStatusUsuarioAsync</c>.
    /// </summary>
    private static async Task<Ok<FilialResponse>> AlterarCelulasAtivasAsync(
        Guid id,
        [FromBody] AlterarCelulasAtivasRequest? request,
        [FromServices] ICommandHandler<AlterarCelulasAtivasCommand, FilialResponse> handler,
        [FromServices] IValidator<AlterarCelulasAtivasCommand> validator,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw ValidationProblems.BodyAusente(
                MensagemPayloadInvalido,
                "Corpo da requisição ausente ou malformado.");
        }

        var command = new AlterarCelulasAtivasCommand(id, request.CelulasAtivas);

        var resultado = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        ValidationProblems.EnsureValid(resultado, MensagemPayloadInvalido);

        var resposta = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(resposta);
    }
}
