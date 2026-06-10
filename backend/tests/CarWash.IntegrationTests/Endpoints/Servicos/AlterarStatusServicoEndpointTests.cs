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
public class AlterarStatusServicoEndpointTests : IAsyncDisposable
{
    private static readonly Uri RotaCriar = new("/api/v1/servicos", UriKind.Relative);

    private readonly CarWashWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public AlterarStatusServicoEndpointTests(PostgresFixture fixture)
    {
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    [Fact]
    public async Task PATCH_ativo_false_inativa_servico_e_retorna_estado()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var id = await CadastrarAsync(client);

        var resp = await client.PatchAsJsonAsync(RotaStatus(id), new { ativo = false }, _json);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var corpo = await resp.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("id").GetGuid().Should().Be(id);
        corpo.GetProperty("ativo").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task PATCH_body_vazio_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var id = await CadastrarAsync(client);

        var resp = await client.PatchAsJsonAsync(RotaStatus(id), new { }, _json);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PATCH_id_inexistente_retorna_404()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var resp = await client.PatchAsJsonAsync(RotaStatus(Guid.NewGuid()), new { ativo = false }, _json);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PATCH_sem_token_retorna_401()
    {
        var client = _factory.CreateClient();
        var resp = await client.PatchAsJsonAsync(RotaStatus(Guid.NewGuid()), new { ativo = false }, _json);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static Uri RotaStatus(Guid id) =>
        new($"/api/v1/servicos/{id}/status", UriKind.Relative);

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
