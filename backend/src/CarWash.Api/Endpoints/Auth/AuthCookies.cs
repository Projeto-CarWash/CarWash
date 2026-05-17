using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace CarWash.Api.Endpoints.Auth;

/// <summary>
/// Helper de leitura/escrita do cookie httpOnly do refresh token.
///
/// <para>Política do cookie:</para>
/// <list type="bullet">
///   <item><c>HttpOnly = true</c> — inacessível ao JavaScript (mitiga XSS).</item>
///   <item><c>Secure = true</c> em qualquer ambiente diferente de Development.</item>
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
            new CookieOptions
            {
                HttpOnly = true,
                Secure = !env.IsDevelopment(),
                SameSite = SameSiteMode.Strict,
                Path = CookiePath,
                Expires = new DateTimeOffset(expiraEm, TimeSpan.Zero),
                IsEssential = true,
            });
    }

    public static void ApagarRefreshCookie(HttpContext context, IHostEnvironment env)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(env);

        context.Response.Cookies.Append(
            RefreshTokenCookieName,
            string.Empty,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = !env.IsDevelopment(),
                SameSite = SameSiteMode.Strict,
                Path = CookiePath,
                Expires = DateTimeOffset.UnixEpoch,
                IsEssential = true,
            });
    }

    public static string? LerRefreshCookie(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Request.Cookies.TryGetValue(RefreshTokenCookieName, out var token)
            && !string.IsNullOrWhiteSpace(token))
        {
            return token;
        }

        return null;
    }
}
