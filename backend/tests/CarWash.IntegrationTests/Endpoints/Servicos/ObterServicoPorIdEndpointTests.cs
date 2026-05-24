using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarWash.IntegrationTests.Collections;
using CarWash.IntegrationTests.Fixtures;
using CarWash.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CarWash.IntegrationTests.Endpoints.Servicos;

[Collection(nameof(PostgresCollection))]
public class ObterServicoPorIdEndpointTests : IAsyncDisposable
{
    private readonly CarWashWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public ObterServicoPorIdEndpointTests(PostgresFixture fixture)
    {
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    [Fact]
    public async Task GET_id_inexistente_retorna_404_canonico()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var id = Guid.NewGuid();
        var response = await client.GetAsync(new Uri($"/api/v1/servicos/{id}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("title").GetString().Should().Be("Serviço não encontrado.");
        corpo.GetProperty("type").GetString().Should().Contain("not-found");
        corpo.GetProperty("correlationId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GET_id_malformado_retorna_404_canonico()
    {
        // A rota tem constraint `{id:guid}`. Quando o valor não bate (ex.: "abc"),
        // o router não acha o endpoint e o middleware UseStatusCodePages emite
        // ProblemDetails 404 canônico.
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var response = await client.GetAsync(new Uri("/api/v1/servicos/abc", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_sem_token_retorna_401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(new Uri($"/api/v1/servicos/{Guid.NewGuid()}", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
