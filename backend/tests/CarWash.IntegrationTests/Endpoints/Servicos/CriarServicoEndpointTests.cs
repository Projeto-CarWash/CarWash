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
public class CriarServicoEndpointTests : IAsyncDisposable
{
    private static readonly Uri RotaCriar = new("/api/v1/servicos", UriKind.Relative);

    private readonly CarWashWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public CriarServicoEndpointTests(PostgresFixture fixture)
    {
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    [Fact]
    public async Task POST_valido_retorna_201_com_Location()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var response = await client.PostAsJsonAsync(RotaCriar, PayloadValido(), _json);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.OriginalString.Should().StartWith("/api/v1/servicos/");

        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("id").GetGuid().Should().NotBeEmpty();
        corpo.GetProperty("mensagem").GetString().Should().Be("Serviço cadastrado com sucesso.");
        corpo.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task POST_sem_token_retorna_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(RotaCriar, PayloadValido(), _json);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_body_vazio_retorna_400_com_problem_details()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var response = await client.PostAsJsonAsync(RotaCriar, new { }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.TryGetProperty("errors", out _).Should().BeTrue();
    }

    [Fact]
    public async Task POST_preco_zero_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var payload = PayloadValido();
        payload["preco"] = 0;

        var response = await client.PostAsJsonAsync(RotaCriar, payload, _json);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_nome_duplicado_retorna_409()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        string nome = $"Serviço-{Guid.NewGuid():N}";
        var primeiroPayload = PayloadValido();
        primeiroPayload["nome"] = nome;

        var primeiro = await client.PostAsJsonAsync(RotaCriar, primeiroPayload, _json);
        primeiro.StatusCode.Should().Be(HttpStatusCode.Created);

        // Mesmo nome, preço/duração diferentes para garantir conflito por nome.
        var segundoPayload = PayloadValido();
        segundoPayload["nome"] = nome;
        segundoPayload["preco"] = 99.99m;
        segundoPayload["duracaoMin"] = 60;
        var segundo = await client.PostAsJsonAsync(RotaCriar, segundoPayload, _json);

        segundo.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var corpo = await segundo.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("type").GetString().Should().Contain("servico-nome-duplicado");
    }

    private static Dictionary<string, object?> PayloadValido() => new()
    {
        ["nome"] = $"Serviço-{Guid.NewGuid():N}",
        ["preco"] = 30.00m,
        ["duracaoMin"] = 30,
    };

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
