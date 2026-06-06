using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace CarWash.Api.Endpoints.Auth;

/// <summary>
/// Helper de leitura/escrita do cookie httpOnly do refresh token.
///
/// <para>Política do cookie:</para>
/// <list type="bullet">
///   <item><c>HttpOnly = true</c> — inacessível ao JavaScript (mitiga XSS).</item>
///   <item><c>Secure = true</c> apenas em prod/hom (qualquer ambiente que não seja Development nem Testing).</item>
///   <item><c>SameSite = Strict</c> — mitiga CSRF clássico.</item>
///   <item><c>Path = "/api/v1/auth"</c> — cookie só é enviado em endpoints de auth.</item>
/// </list>
/// </summary>
public static class AuthCookies
{
    public const string RefreshTokenCookieName = "carwash_refresh_token";
    public const string CookiePath = "/api/v1/auth";

    public static void EscreverRefreshCookie(
        HttpContext context,
        IHostEnvironment env,
        string tokenBruto,
        DateTime expiraEm)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(env);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenBruto);

        context.Response.Cookies.Append(
            RefreshTokenCookieName,
            tokenBruto,
            BuildOptions(env, new DateTimeOffset(expiraEm, TimeSpan.Zero)));
    }

    public static void ApagarRefreshCookie(HttpContext context, IHostEnvironment env)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(env);

        context.Response.Cookies.Append(
            RefreshTokenCookieName,
            string.Empty,
            BuildOptions(env, DateTimeOffset.UnixEpoch));
    }

    public static string? LerRefreshCookie(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Request.Cookies.TryGetValue(RefreshTokenCookieName, out string? token)
            && !string.IsNullOrWhiteSpace(token))
        {
            return token;
        }

        return null;
    }

    private static CookieOptions BuildOptions(IHostEnvironment env, DateTimeOffset expires) => new()
    {
        HttpOnly = true,
        Secure = ShouldUseSecure(env),
        SameSite = SameSiteMode.Strict,
        Path = CookiePath,
        Expires = expires,
        IsEssential = true,
    };

    /// <summary>
    /// Habilita <c>Secure</c> apenas quando o ambiente realmente tem TLS:
    /// nginx em hom/prod faz terminate de TLS e repassa HTTP para o backend.
    /// Em Development e Testing o cookie precisa funcionar sob HTTP local.
    /// </summary>
    private static bool ShouldUseSecure(IHostEnvironment env) =>
        !env.IsDevelopment() && !env.IsEnvironment("Testing");
}
