using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarWash.IntegrationTests.Collections;
using CarWash.IntegrationTests.Fixtures;
using CarWash.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CarWash.IntegrationTests.Endpoints.Filiais;

/// <summary>
/// Cobertura de integração do <c>GET /api/v1/filiais</c> (RF017 — listagem
/// pública para destravar o frontend). Cobre 200 + envelope, filtro
/// <c>?ativo=true</c>, paginação inválida (400) e 401 sem token.
/// </summary>
[Collection(nameof(PostgresCollection))]
public class ListarFiliaisEndpointTests : IAsyncDisposable
{
    private static readonly Uri RotaBase = new("/api/v1/filiais", UriKind.Relative);

    private readonly CarWashWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public ListarFiliaisEndpointTests(PostgresFixture fixture)
    {
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    [Fact]
    public async Task GET_lista_retorna_200_com_envelope_paginado()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        // Cadastra ao menos uma filial para a lista nunca estar vazia neste
        // contexto (banco compartilhado por collection — outros testes podem
        // limpar/preencher, então não asseguramos "exatamente N", apenas
        // a forma do envelope).
        await client.PostAsJsonAsync(RotaBase, new
        {
            nome = $"FilialList{Guid.NewGuid():N}"[..30],
            codigo = $"L{Guid.NewGuid():N}"[..10].ToUpperInvariant(),
            celulasAtivas = 8,
        }, _json);

        var response = await client.GetAsync(RotaBase);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.TryGetProperty("itens", out _).Should().BeTrue();
        corpo.GetProperty("pagina").GetInt32().Should().BeGreaterOrEqualTo(1);
        corpo.GetProperty("tamanhoPagina").GetInt32().Should().BeInRange(1, 100);
        corpo.GetProperty("total").GetInt32().Should().BeGreaterOrEqualTo(1);

        var itens = corpo.GetProperty("itens");
        itens.GetArrayLength().Should().BeGreaterThan(0);
        var primeiro = itens[0];
        primeiro.TryGetProperty("id", out _).Should().BeTrue();
        primeiro.TryGetProperty("nome", out _).Should().BeTrue();
        primeiro.TryGetProperty("ativo", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GET_filtro_ativo_true_retorna_apenas_ativas()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        await client.PostAsJsonAsync(RotaBase, new
        {
            nome = $"FilialAtivaList{Guid.NewGuid():N}"[..30],
            codigo = $"A{Guid.NewGuid():N}"[..10].ToUpperInvariant(),
            celulasAtivas = 5,
        }, _json);

        var response = await client.GetAsync(new Uri("/api/v1/filiais?ativo=true", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        var itens = corpo.GetProperty("itens");

        // Todas as itens devem ter ativo == true.
        foreach (var item in itens.EnumerateArray())
        {
            item.GetProperty("ativo").GetBoolean().Should().BeTrue();
        }
    }

    [Fact]
    public async Task GET_pagina_zero_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var response = await client.GetAsync(new Uri("/api/v1/filiais?pagina=0", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("errors").GetProperty("pagina")[0].GetString()
            .Should().Contain("maior ou igual a 1");
    }

    [Fact]
    public async Task GET_tamanho_acima_de_100_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var response = await client.GetAsync(new Uri("/api/v1/filiais?tamanhoPagina=999", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_sem_token_retorna_401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(RotaBase);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
