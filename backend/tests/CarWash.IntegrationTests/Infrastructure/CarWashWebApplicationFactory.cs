using CarWash.Infrastructure.Persistence;
using CarWash.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace CarWash.IntegrationTests.Infrastructure;

public class CarWashWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly PostgresFixture _fixture;

    public CarWashWebApplicationFactory(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task EnsureDatabaseCreatedAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CarWashDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _fixture.ConnectionString);
    }
}
