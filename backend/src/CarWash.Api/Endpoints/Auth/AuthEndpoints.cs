using CarWash.Api.Filters;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Auth.Login;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace CarWash.Api.Endpoints.Auth;

/// <summary>
/// Endpoints de autenticação (Task 3). Sempre <c>Cache-Control: no-store</c>
/// para evitar caching intermediário do token.
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuth(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var grupo = app.MapGroup("/api/v1/auth")
            .WithTags("Auth");

        grupo.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .AddEndpointFilter<ValidationFilter<LoginCommand>>()
            .WithName("AuthLogin")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<Ok<LoginResponse>> LoginAsync(
        [FromBody] LoginCommand command,
        [FromServices] ICommandHandler<LoginCommand, LoginResponse> handler,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var resposta = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);
        http.Response.Headers[HeaderNames.CacheControl] = "no-store";
        return TypedResults.Ok(resposta);
    }
}
