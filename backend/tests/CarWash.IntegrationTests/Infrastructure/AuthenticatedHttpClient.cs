using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CarWash.IntegrationTests.Fixtures;

namespace CarWash.IntegrationTests.Infrastructure;

/// <summary>
/// Helper que loga como <c>admin@carwash.local</c> (seed migration + senha vinda
/// do <see cref="PostgresFixture.SeedAdminPassword"/>) e devolve um <see cref="HttpClient"/>
/// com <c>Authorization: Bearer {accessToken}</c> pronto para chamar endpoints
/// que exigem <c>RequireAuthorization</c>.
/// </summary>
public static class AuthenticatedHttpClient
{
    private static readonly Uri RotaLogin = new("/api/v1/auth/login", UriKind.Relative);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static async Task<HttpClient> CreateAsync(CarWashWebApplicationFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var client = factory.CreateClient();

        var login = await client.PostAsJsonAsync(RotaLogin, new
        {
            email = "admin@carwash.local",
            senha = PostgresFixture.SeedAdminPassword,
        }, Json).ConfigureAwait(false);

        login.EnsureSuccessStatusCode();

        var body = await login.Content.ReadFromJsonAsync<JsonElement>(Json).ConfigureAwait(false);
        var accessToken = body.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("Login não devolveu accessToken.");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }
}
