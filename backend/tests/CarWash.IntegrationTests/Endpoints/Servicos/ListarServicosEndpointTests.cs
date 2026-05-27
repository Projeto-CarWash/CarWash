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
public class ListarServicosEndpointTests : IAsyncDisposable
{
    private static readonly Uri RotaBase = new("/api/v1/servicos", UriKind.Relative);

    private readonly CarWashWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public ListarServicosEndpointTests(PostgresFixture fixture)
    {
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    [Fact]
    public async Task GET_lista_retorna_200_com_servicos_seed()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var response = await client.GetAsync(RotaBase);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.TryGetProperty("itens", out _).Should().BeTrue();
        corpo.GetProperty("pagina").GetInt32().Should().BeGreaterOrEqualTo(1);
        corpo.GetProperty("tamanhoPagina").GetInt32().Should().BeInRange(1, 100);
    }

    [Fact]
    public async Task GET_pagina_zero_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var response = await client.GetAsync(new Uri("/api/v1/servicos?pagina=0", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("errors").GetProperty("pagina")[0].GetString()
            .Should().Contain("maior ou igual a 1");
    }

    [Fact]
    public async Task GET_tamanho_acima_de_100_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var response = await client.GetAsync(new Uri("/api/v1/servicos?tamanhoPagina=999", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_sem_token_retorna_401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(RotaBase);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
