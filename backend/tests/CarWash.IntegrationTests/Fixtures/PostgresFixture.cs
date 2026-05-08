using Testcontainers.PostgreSql;
using Xunit;

namespace CarWash.IntegrationTests.Fixtures;

public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("carwash")
        .WithUsername("carwash_owner")
        .WithPassword("test")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    /// <inheritdoc/>
    public Task InitializeAsync() => _container.StartAsync();

    /// <inheritdoc/>
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
