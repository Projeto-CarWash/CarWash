using CarWash.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CarWash.IntegrationTests.Infrastructure;

/// <summary>
/// Variante da factory de teste que roda em <c>Development</c> para habilitar
/// o gerador Swagger (regular factory usa <c>Testing</c> onde Swagger é desabilitado).
/// </summary>
public class SwaggerWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly PostgresFixture _fixture;

    public SwaggerWebApplicationFactory(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Default", _fixture.ConnectionString);
        builder.UseSetting("Jwt:Secret", CarWashWebApplicationFactory.JwtTestingSecret);
        builder.UseSetting("Jwt:Issuer", "carwash-api");
        builder.UseSetting("Jwt:Audience", "carwash-web");
        builder.UseSetting("Cors:AllowedOrigins:0", "http://localhost");
    }
}
