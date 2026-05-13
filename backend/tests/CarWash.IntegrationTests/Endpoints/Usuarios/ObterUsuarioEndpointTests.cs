using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarWash.IntegrationTests.Collections;
using CarWash.IntegrationTests.Fixtures;
using CarWash.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CarWash.IntegrationTests.Endpoints.Usuarios;

[Collection(nameof(PostgresCollection))]
public class ObterUsuarioEndpointTests : IAsyncDisposable
{
    private readonly CarWashWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public ObterUsuarioEndpointTests(PostgresFixture fixture)
    {
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    [Fact]
    public async Task GET_id_inexistente_retorna_404()
    {
        var client = _factory.CreateClient();
        var id = Guid.NewGuid();

        var response = await client.GetAsync(new Uri($"/api/v1/usuarios/{id}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("title").GetString().Should().Be("Usuário não encontrado.");
        corpo.GetProperty("type").GetString().Should().Contain("not-found");
        corpo.GetProperty("correlationId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GET_id_malformado_retorna_400()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/v1/usuarios/abc", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
