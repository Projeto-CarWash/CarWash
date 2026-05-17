using CarWash.Api.Filters;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Auth.Login;
using CarWash.Application.Auth.Logout;
using CarWash.Application.Auth.Refresh;
using CarWash.Application.Common.Exceptions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;

namespace CarWash.Api.Endpoints.Auth;

/// <summary>
/// Endpoints de autenticação (RF001). Sempre <c>Cache-Control: no-store</c>
/// para evitar caching intermediário do token.
///
/// <para>
/// O refresh token vive APENAS no cookie httpOnly <c>carwash_refresh_token</c>
/// (Secure em hom/prod, SameSite=Strict, Path=/api/v1/auth). Nunca no body.
/// </para>
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
            .RequireRateLimiting("auth-login")
            .AddEndpointFilter<ValidationFilter<LoginCommand>>()
            .WithName("AuthLogin")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        grupo.MapPost("/refresh", RefreshAsync)
            .AllowAnonymous()
            .WithName("AuthRefresh")
            .Produces<RefreshResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        grupo.MapPost("/logout", LogoutAsync)
            .AllowAnonymous()
            .WithName("AuthLogout")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<Ok<LoginResponse>> LoginAsync(
        [FromBody] LoginCommand command,
        [FromServices] ICommandHandler<LoginCommand, LoginResultado> handler,
        [FromServices] IHostEnvironment env,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        // Aplica `Cache-Control: no-store` ANTES de qualquer execução do handler para
        // garantir o header tanto no caminho de sucesso quanto no de exceção. O
        // middleware de exceção preserva headers já definidos (ProblemDetails apenas
        // Clear o response stream — o header sobrevive).
        http.Response.Headers[HeaderNames.CacheControl] = "no-store";

        var resultado = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        AuthCookies.EscreverRefreshCookie(http, env, resultado.RefreshToken, resultado.RefreshExpiresAt);

        return TypedResults.Ok(LoginResponse.From(resultado));
    }

    private static async Task<Ok<RefreshResponse>> RefreshAsync(
        [FromServices] ICommandHandler<RefreshCommand, RefreshResultado> handler,
        [FromServices] IHostEnvironment env,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        // Importante: setar `Cache-Control: no-store` ANTES de ler o cookie/lançar a
        // exceção 401, para que TODA resposta (200 ou 401) carregue o header — exigido
        // por RFC 6265 e CA011 (não cachear tokens de sessão).
        http.Response.Headers[HeaderNames.CacheControl] = "no-store";

        var refreshToken = AuthCookies.LerRefreshCookie(http)
            ?? throw new RefreshTokenInvalidoException();

        var resultado = await handler.HandleAsync(new RefreshCommand(refreshToken), cancellationToken).ConfigureAwait(false);

        AuthCookies.EscreverRefreshCookie(http, env, resultado.RefreshToken, resultado.RefreshExpiresAt);

        return TypedResults.Ok(RefreshResponse.From(resultado));
    }

    private static async Task<NoContent> LogoutAsync(
        [FromServices] ICommandHandler<LogoutCommand, LogoutResultado> handler,
        [FromServices] IHostEnvironment env,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        http.Response.Headers[HeaderNames.CacheControl] = "no-store";

        var refreshToken = AuthCookies.LerRefreshCookie(http);

        await handler.HandleAsync(new LogoutCommand(refreshToken), cancellationToken).ConfigureAwait(false);

        AuthCookies.ApagarRefreshCookie(http, env);

        return TypedResults.NoContent();
    }
}
