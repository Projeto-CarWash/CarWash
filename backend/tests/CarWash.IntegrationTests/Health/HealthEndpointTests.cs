using CarWash.IntegrationTests.Collections;
using CarWash.IntegrationTests.Fixtures;
using CarWash.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CarWash.IntegrationTests.Health;

[Collection(nameof(PostgresCollection))]
public class HealthEndpointTests : IAsyncDisposable
{
    private readonly CarWashWebApplicationFactory _factory;

    public HealthEndpointTests(PostgresFixture fixture)
    {
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    [Fact]
    public async Task GET_health_returns_200_Healthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health", UriKind.Relative));
        string body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        body.Should().Contain("Healthy");
    }

    [Fact]
    public async Task GET_health_ready_returns_200_when_postgres_is_up()
    {
        await _factory.EnsureDatabaseCreatedAsync();
        var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
