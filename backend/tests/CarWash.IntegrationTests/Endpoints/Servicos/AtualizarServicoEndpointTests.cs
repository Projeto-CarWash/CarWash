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
public class AtualizarServicoEndpointTests : IAsyncDisposable
{
    private static readonly Uri RotaCriar = new("/api/v1/servicos", UriKind.Relative);

    private readonly CarWashWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public AtualizarServicoEndpointTests(PostgresFixture fixture)
    {
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    [Fact]
    public async Task PATCH_atualiza_dados_retorna_200()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var id = await CadastrarAsync(client);

        var payload = new Dictionary<string, object?>
        {
            ["nome"] = "Lavagem Premium Atualizada",
            ["preco"] = 55.00m,
            ["duracaoMin"] = 50,
        };

        var response = await client.PatchAsJsonAsync(new Uri($"/api/v1/servicos/{id}", UriKind.Relative), payload, _json);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("id").GetGuid().Should().Be(id);
        corpo.GetProperty("nome").GetString().Should().Be("Lavagem Premium Atualizada");
        corpo.GetProperty("preco").GetDecimal().Should().Be(55.00m);
        corpo.GetProperty("duracaoMin").GetInt32().Should().Be(50);
    }

    [Fact]
    public async Task PATCH_id_inexistente_retorna_404()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var payload = new Dictionary<string, object?>
        {
            ["nome"] = "Serviço Inexistente",
            ["preco"] = 10.00m,
            ["duracaoMin"] = 15,
        };

        var response = await client.PatchAsJsonAsync(
            new Uri($"/api/v1/servicos/{Guid.NewGuid()}", UriKind.Relative),
            payload,
            _json);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PATCH_payload_invalido_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var id = await CadastrarAsync(client);

        var response = await client.PatchAsJsonAsync(
            new Uri($"/api/v1/servicos/{id}", UriKind.Relative),
            new { },
            _json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<Guid> CadastrarAsync(HttpClient client)
    {
        var payload = new Dictionary<string, object?>
        {
            ["nome"] = $"Serviço-{Guid.NewGuid():N}",
            ["preco"] = 30.00m,
            ["duracaoMin"] = 30,
        };

        var response = await client.PostAsJsonAsync(RotaCriar, payload, _json);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        return corpo.GetProperty("id").GetGuid();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
