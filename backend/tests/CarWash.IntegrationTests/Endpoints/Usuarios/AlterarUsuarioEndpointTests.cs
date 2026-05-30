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
public class AlterarUsuarioEndpointTests : IAsyncDisposable
{
    private static readonly Uri RotaCriar = new("/api/v1/usuarios", UriKind.Relative);

    private readonly CarWashWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public AlterarUsuarioEndpointTests(PostgresFixture fixture)
    {
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    [Fact]
    public async Task PUT_valido_retorna_200_com_dados_atualizados()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var id = await CadastrarAsync(client);

        var novoEmail = $"alterado-{Guid.NewGuid():N}@carwash.local";
        var response = await client.PutAsJsonAsync(
            new Uri($"/api/v1/usuarios/{id}", UriKind.Relative),
            new
            {
                nome = "Alice Alterada",
                email = novoEmail,
                perfil = "Admin",
            },
            _json);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("id").GetGuid().Should().Be(id);
        corpo.GetProperty("nome").GetString().Should().Be("Alice Alterada");
        corpo.GetProperty("email").GetString().Should().Be(novoEmail);
        corpo.GetProperty("perfil").GetString().Should().Be("Admin");
    }

    [Fact]
    public async Task PUT_id_inexistente_retorna_404()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var response = await client.PutAsJsonAsync(
            new Uri($"/api/v1/usuarios/{Guid.NewGuid()}", UriKind.Relative),
            new
            {
                nome = "Alice",
                email = $"alterado-{Guid.NewGuid():N}@carwash.local",
                perfil = "Funcionario",
            },
            _json);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_payload_invalido_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var id = await CadastrarAsync(client);

        var response = await client.PutAsJsonAsync(
            new Uri($"/api/v1/usuarios/{id}", UriKind.Relative),
            new { nome = "ab", email = "naoeemail", perfil = "Inexistente" },
            _json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PUT_email_em_uso_por_outro_usuario_retorna_409()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var emailOutro = $"outro-{Guid.NewGuid():N}@carwash.local";
        await CadastrarAsync(client, emailOutro);
        var id = await CadastrarAsync(client);

        var response = await client.PutAsJsonAsync(
            new Uri($"/api/v1/usuarios/{id}", UriKind.Relative),
            new
            {
                nome = "Alice",
                email = emailOutro,
                perfil = "Funcionario",
            },
            _json);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PUT_sem_token_retorna_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            new Uri($"/api/v1/usuarios/{Guid.NewGuid()}", UriKind.Relative),
            new { nome = "x", email = "x@x.com", perfil = "Funcionario" },
            _json);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<Guid> CadastrarAsync(HttpClient client, string? emailForcado = null)
    {
        var email = emailForcado ?? $"alice-{Guid.NewGuid():N}@carwash.local";
        var resp = await client.PostAsJsonAsync(RotaCriar, new
        {
            nome = "Alice",
            email,
            senha = "Senha1234",
            perfil = "Funcionario",
        }, _json);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var corpo = await resp.Content.ReadFromJsonAsync<JsonElement>(_json);
        return corpo.GetProperty("id").GetGuid();
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
