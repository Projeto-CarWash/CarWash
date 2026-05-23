using CarWash.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CarWash.IntegrationTests.Infrastructure;

public class CarWashWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly PostgresFixture _fixture;

    public CarWashWebApplicationFactory(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Secret JWT determinístico para integração — ≥ 32 bytes para HMAC-SHA256.
    /// Apenas testes; nunca usar em prod.
    /// </summary>
    internal const string JwtTestingSecret =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _fixture.ConnectionString);

        // Secret JWT obrigatório no startup; passamos um valor fixo para os testes.
        builder.UseSetting("Jwt:Secret", JwtTestingSecret);
        builder.UseSetting("Jwt:Issuer", "carwash-api");
        builder.UseSetting("Jwt:Audience", "carwash-web");
        builder.UseSetting("Jwt:AccessTokenValiditySeconds", "900");
        builder.UseSetting("Jwt:RefreshTokenValiditySeconds", "604800");

        // CORS: pelo menos uma origem para não conflitar com AllowCredentials.
        builder.UseSetting("Cors:AllowedOrigins:0", "http://localhost");
    }
}
