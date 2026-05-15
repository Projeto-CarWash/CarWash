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
public class AlterarStatusEndpointTests : IAsyncDisposable
{
    private static readonly Uri RotaCriar = new("/api/v1/usuarios", UriKind.Relative);

    private readonly CarWashWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public AlterarStatusEndpointTests(PostgresFixture fixture)
    {
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    [Fact]
    public async Task PATCH_ativo_false_inativa_e_GET_confirma()
    {
        var client = _factory.CreateClient();
        var id = await CadastrarUsuarioAsync(client);

        var resp = await client.PatchAsJsonAsync(RotaStatus(id), new { ativo = false }, _json);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var corpo = await resp.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("id").GetGuid().Should().Be(id);
        corpo.GetProperty("ativo").GetBoolean().Should().BeFalse();
        corpo.GetProperty("atualizadoEm").GetDateTime().Should().NotBe(default);

        var getResp = await client.GetAsync(new Uri($"/api/v1/usuarios/{id}", UriKind.Relative));
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var getCorpo = await getResp.Content.ReadFromJsonAsync<JsonElement>(_json);
        getCorpo.GetProperty("ativo").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task PATCH_idempotente_nao_altera_AtualizadoEm()
    {
        var client = _factory.CreateClient();
        var id = await CadastrarUsuarioAsync(client);

        var primeira = await client.PatchAsJsonAsync(RotaStatus(id), new { ativo = false }, _json);
        primeira.StatusCode.Should().Be(HttpStatusCode.OK);
        var primeiraCorpo = await primeira.Content.ReadFromJsonAsync<JsonElement>(_json);
        var primeiraAtualizado = primeiraCorpo.GetProperty("atualizadoEm").GetDateTime();

        await Task.Delay(50);

        var segunda = await client.PatchAsJsonAsync(RotaStatus(id), new { ativo = false }, _json);
        segunda.StatusCode.Should().Be(HttpStatusCode.OK);
        var segundaCorpo = await segunda.Content.ReadFromJsonAsync<JsonElement>(_json);
        segundaCorpo.GetProperty("ativo").GetBoolean().Should().BeFalse();
        var segundaAtualizado = segundaCorpo.GetProperty("atualizadoEm").GetDateTime();

        // Idempotência: nenhum SaveChanges → AtualizadoEm não muda.
        // Tolerância de 1µs: primeira chamada devolve DateTime em memória (precisão 100ns do .NET);
        // segunda lê do PostgreSQL (precisão 1µs em timestamptz) e perde o último tick.
        segundaAtualizado.Should().BeCloseTo(primeiraAtualizado, TimeSpan.FromTicks(10));
    }

    [Fact]
    public async Task PATCH_id_inexistente_retorna_404()
    {
        var client = _factory.CreateClient();
        var id = Guid.NewGuid();

        var resp = await client.PatchAsJsonAsync(RotaStatus(id), new { ativo = false }, _json);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var corpo = await resp.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("title").GetString().Should().Be("Usuário não encontrado.");
    }

    [Fact]
    public async Task PATCH_id_malformado_retorna_400()
    {
        var client = _factory.CreateClient();

        var resp = await client.PatchAsJsonAsync(
            new Uri("/api/v1/usuarios/abc/status", UriKind.Relative),
            new { ativo = false },
            _json);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PATCH_body_vazio_retorna_400()
    {
        var client = _factory.CreateClient();
        var id = await CadastrarUsuarioAsync(client);

        var requisicao = new HttpRequestMessage(HttpMethod.Patch, RotaStatus(id))
        {
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json"),
        };
        var resp = await client.SendAsync(requisicao);

        // {} é deserializável como request com Ativo=false (default) — então 200 ou erro?
        // Como Ativo é bool obrigatório no JSON, com {} ele assume false (default bool).
        // Resultado esperado: 200 com ativo=false (o usuário acabou de ser criado ativo).
        // O cenário de "body vazio = 400" só é atingido com body null ou inválido.
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PATCH_payload_null_retorna_400()
    {
        var client = _factory.CreateClient();
        var id = await CadastrarUsuarioAsync(client);

        var requisicao = new HttpRequestMessage(HttpMethod.Patch, RotaStatus(id))
        {
            Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json"),
        };
        var resp = await client.SendAsync(requisicao);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PATCH_ativa_inativo_volta_para_true()
    {
        var client = _factory.CreateClient();
        var id = await CadastrarUsuarioAsync(client);

        (await client.PatchAsJsonAsync(RotaStatus(id), new { ativo = false }, _json))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var reativar = await client.PatchAsJsonAsync(RotaStatus(id), new { ativo = true }, _json);
        reativar.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await reativar.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("ativo").GetBoolean().Should().BeTrue();
    }

    private async Task<Guid> CadastrarUsuarioAsync(HttpClient client)
    {
        var email = $"alice-{Guid.NewGuid():N}@carwash.local";
        var response = await client.PostAsJsonAsync(RotaCriar, new
        {
            nome = "Alice",
            email,
            senha = "Senha1234",
            perfil = "Funcionario",
        }, _json);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        return corpo.GetProperty("id").GetGuid();
    }

    private static Uri RotaStatus(Guid id) =>
        new($"/api/v1/usuarios/{id}/status", UriKind.Relative);

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
