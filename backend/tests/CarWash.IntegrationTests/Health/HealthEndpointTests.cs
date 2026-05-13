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
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        body.Should().Contain("Healthy");
    }

    [Fact]
    public async Task GET_health_ready_returns_200_when_postgres_is_up()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_health_with_valid_correlation_id_echoes_header()
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/health", UriKind.Relative));
        request.Headers.Add("X-Correlation-Id", "req-123_ABC.xyz");

        var response = await client.SendAsync(request);

        response.Headers.TryGetValues("X-Correlation-Id", out var values).Should().BeTrue();
        values.Should().ContainSingle().Which.Should().Be("req-123_ABC.xyz");
    }

    [Fact]
    public async Task GET_health_with_invalid_correlation_id_generates_new_value()
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/health", UriKind.Relative));
        request.Headers.Add("X-Correlation-Id", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa@");

        var response = await client.SendAsync(request);

        response.Headers.TryGetValues("X-Correlation-Id", out var values).Should().BeTrue();
        var correlationId = values.Should().ContainSingle().Which;
        correlationId.Should().NotBe("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa@");
        Guid.TryParseExact(correlationId, "N", out _).Should().BeTrue();
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
