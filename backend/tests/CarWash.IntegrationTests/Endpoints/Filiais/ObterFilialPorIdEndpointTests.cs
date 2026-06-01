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
/// RF018 — <c>GET /api/v1/filiais/{id}</c> ponta a ponta. Apenas autenticação
/// (qualquer perfil); 200/404/401.
/// </summary>
[Collection(nameof(PostgresCollection))]
public class ObterFilialPorIdEndpointTests : IAsyncDisposable
{
    private static readonly Uri RotaFiliais = new("/api/v1/filiais", UriKind.Relative);

    private readonly CarWashWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public ObterFilialPorIdEndpointTests(PostgresFixture fixture)
    {
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    [Fact]
    public async Task GET_admin_existente_retorna_200_com_response()
    {
        var admin = await AuthenticatedHttpClient.CreateAsync(_factory);
        var id = await CriarFilialAsync(admin, celulas: 8);

        var get = await admin.GetAsync(RotaPorId(id));

        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await get.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("id").GetGuid().Should().Be(id);
        corpo.GetProperty("celulasAtivas").GetInt32().Should().Be(8);
        corpo.GetProperty("ativa").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GET_funcionario_existente_retorna_200()
    {
        // GET é apenas autenticado — funcionário (não-Admin) tem acesso.
        var admin = await AuthenticatedHttpClient.CreateAsync(_factory);
        var id = await CriarFilialAsync(admin, celulas: 3);

        var funcionario = await FuncionarioHttpClient.CreateAsync(_factory);
        var get = await funcionario.GetAsync(RotaPorId(id));

        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await get.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("id").GetGuid().Should().Be(id);
    }

    [Fact]
    public async Task GET_inexistente_retorna_404_com_mensagem_exata()
    {
        var admin = await AuthenticatedHttpClient.CreateAsync(_factory);

        var get = await admin.GetAsync(RotaPorId(Guid.NewGuid()));

        get.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var corpo = await get.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("title").GetString().Should().Be("Filial não encontrada.");
        corpo.GetProperty("type").GetString().Should().Contain("not-found");
    }

    [Fact]
    public async Task GET_sem_token_retorna_401()
    {
        var anonimo = _factory.CreateClient();

        var get = await anonimo.GetAsync(RotaPorId(Guid.NewGuid()));

        get.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<Guid> CriarFilialAsync(HttpClient adminClient, int celulas)
    {
        var nome = $"Filial {Guid.NewGuid():N}"[..30];
        var codigo = $"F{Guid.NewGuid():N}"[..8].ToUpperInvariant();
        var response = await adminClient.PostAsJsonAsync(RotaFiliais, new { nome, codigo, celulasAtivas = celulas }, _json);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>(_json)).GetProperty("id").GetGuid();
    }

    private static Uri RotaPorId(Guid id) =>
        new($"/api/v1/filiais/{id}", UriKind.Relative);

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
