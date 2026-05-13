using CarWash.Api.Filters;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Usuarios.AlterarStatus;
using CarWash.Application.Usuarios.Common;
using CarWash.Application.Usuarios.CriarUsuario;
using CarWash.Application.Usuarios.ObterUsuarioPorId;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ValidationException = CarWash.Application.Common.Exceptions.ValidationException;

namespace CarWash.Api.Endpoints.Usuarios;

/// <summary>
/// Endpoints de usuários internos (Task 1 — CA1..CA11; Task 2 — alteração de status).
/// </summary>
public static class UsuariosEndpoints
{
    private const string MensagemPayloadInvalido =
        "Dados do usuário inválidos. Verifique os campos e tente novamente.";

    public static IEndpointRouteBuilder MapUsuarios(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var grupo = app.MapGroup("/api/v1/usuarios")
            .WithTags("Usuarios");

        grupo.MapPost("/", CriarAsync)
            .AddEndpointFilter<ValidationFilter<CriarUsuarioCommand>>()
            .WithName("CriarUsuario")
            .Produces<UsuarioResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        grupo.MapGet("/{id}", ObterPorIdAsync)
            .WithName("ObterUsuarioPorId")
            .Produces<UsuarioResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        grupo.MapPatch("/{id:guid}/status", AlterarStatusAsync)
            .WithName("AlterarStatusUsuario")
            .Produces<AlterarStatusUsuarioResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<Created<UsuarioResponse>> CriarAsync(
        [FromBody] CriarUsuarioCommand command,
        [FromServices] ICommandHandler<CriarUsuarioCommand, UsuarioResponse> handler,
        CancellationToken cancellationToken)
    {
        var resposta = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);
        var location = $"/api/v1/usuarios/{resposta.Id}";
        return TypedResults.Created(location, resposta);
    }

    private static async Task<Ok<UsuarioResponse>> ObterPorIdAsync(
        Guid id,
        [FromServices] IQueryHandler<ObterUsuarioPorIdQuery, UsuarioResponse> handler,
        CancellationToken cancellationToken)
    {
        var resposta = await handler.HandleAsync(new ObterUsuarioPorIdQuery(id), cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(resposta);
    }

    /// <summary>
    /// <c>PATCH /api/v1/usuarios/{id}/status</c>. O <c>id</c> vem da rota; o body
    /// é <see cref="AlterarStatusUsuarioRequest"/>. O command é montado aqui e
    /// validado inline — o <see cref="ValidationFilter{T}"/> não é aplicável
    /// porque o command não está nos arguments do endpoint.
    /// </summary>
    private static async Task<Ok<AlterarStatusUsuarioResponse>> AlterarStatusAsync(
        Guid id,
        [FromBody] AlterarStatusUsuarioRequest? request,
        [FromServices] ICommandHandler<AlterarStatusUsuarioCommand, AlterarStatusUsuarioResponse> handler,
        [FromServices] IValidator<AlterarStatusUsuarioCommand> validator,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ValidationException(
                MensagemPayloadInvalido,
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["body"] = ["Corpo da requisição ausente ou malformado."],
                });
        }

        var command = new AlterarStatusUsuarioCommand(id, request.Ativo);

        var resultado = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!resultado.IsValid)
        {
            var erros = resultado.Errors
                .GroupBy(e => NormalizarCampo(e.PropertyName), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).Distinct(StringComparer.Ordinal).ToArray(),
                    StringComparer.OrdinalIgnoreCase);
            throw new ValidationException(MensagemPayloadInvalido, erros);
        }

        var resposta = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(resposta);
    }

    private static string NormalizarCampo(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return "body";
        }

        return char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
    }
}
