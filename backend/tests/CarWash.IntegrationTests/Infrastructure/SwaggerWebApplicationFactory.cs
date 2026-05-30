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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Default", _fixture.ConnectionString);
        builder.UseSetting("Jwt:Secret", CarWashWebApplicationFactory.JwtTestingSecret);
        builder.UseSetting("Jwt:Issuer", "carwash-api");
        builder.UseSetting("Jwt:Audience", "carwash-web");

        // RF015: sem a chave dedicada de confirmação o startup falha em fail-fast.
        // Em máquinas com a env Jwt__ConfirmacaoSigningKey exportada o teste passava
        // por acaso; num container limpo (CA011) ele quebrava — espelhamos o setting
        // de CarWashWebApplicationFactory para tornar a suíte reprodutível.
        builder.UseSetting(
            "Jwt:ConfirmacaoSigningKey",
            CarWashWebApplicationFactory.JwtConfirmacaoTestingKey);
        builder.UseSetting("Cors:AllowedOrigins:0", "http://localhost");
    }
}
