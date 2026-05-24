using CarWash.Infrastructure.Persistence;
using CarWash.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace CarWash.IntegrationTests.Fixtures;

/// <summary>
/// Sobe um PostgreSQL real via Testcontainers, define a env obrigatória
/// <c>CARWASH_SEED_ADMIN_PASSWORD</c> e aplica a migration <c>InitialSchema</c>
/// (em vez de <c>EnsureCreated</c>) — garantindo paridade com produção.
/// </summary>
public class PostgresFixture : IAsyncLifetime
{
    public const string SeedAdminPassword = "TestSeedAdmin!2026";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("carwash")
        .WithUsername("carwash_owner")
        .WithPassword("test")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        // P12: a migration falha se a env não estiver definida. Definimos para os testes.
        Environment.SetEnvironmentVariable(SeedPasswordResolver.EnvironmentVariableName, SeedAdminPassword);

        await _container.StartAsync().ConfigureAwait(false);

        var options = new DbContextOptionsBuilder<CarWashDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var db = new CarWashDbContext(options);
        await db.Database.MigrateAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
