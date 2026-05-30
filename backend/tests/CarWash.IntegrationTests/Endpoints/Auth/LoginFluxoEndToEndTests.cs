using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarWash.IntegrationTests.Collections;
using CarWash.IntegrationTests.Fixtures;
using CarWash.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CarWash.IntegrationTests.Endpoints.Auth;

[Collection(nameof(PostgresCollection))]
public class LoginFluxoEndToEndTests : IAsyncDisposable
{
    private static readonly Uri RotaLogin = new("/api/v1/auth/login", UriKind.Relative);
    private static readonly Uri RotaCriar = new("/api/v1/usuarios", UriKind.Relative);

    private readonly CarWashWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public LoginFluxoEndToEndTests(PostgresFixture fixture)
    {
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    [Fact]
    public async Task Cadastra_loga_inativa_falha_reativa_loga()
    {
        var client = _factory.CreateClient();
        using var admin = await AuthenticatedHttpClient.CreateAsync(_factory);

        string email = $"e2e-{Guid.NewGuid():N}@carwash.local";
        const string senha = "Senha1234";

        var cadastro = await admin.PostAsJsonAsync(RotaCriar, new
        {
            nome = "E2E",
            email,
            senha,
            perfil = "Funcionario",
        }, _json);
        cadastro.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = (await cadastro.Content.ReadFromJsonAsync<JsonElement>(_json)).GetProperty("id").GetGuid();

        var login1 = await client.PostAsJsonAsync(RotaLogin, new { email, senha }, _json);
        login1.StatusCode.Should().Be(HttpStatusCode.OK);

        var inativar = await admin.PatchAsJsonAsync(
            new Uri($"/api/v1/usuarios/{id}/status", UriKind.Relative),
            new { ativo = false },
            _json);
        inativar.StatusCode.Should().Be(HttpStatusCode.OK);

        var login2 = await client.PostAsJsonAsync(RotaLogin, new { email, senha }, _json);
        login2.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var reativar = await admin.PatchAsJsonAsync(
            new Uri($"/api/v1/usuarios/{id}/status", UriKind.Relative),
            new { ativo = true },
            _json);
        reativar.StatusCode.Should().Be(HttpStatusCode.OK);

        var login3 = await client.PostAsJsonAsync(RotaLogin, new { email, senha }, _json);
        login3.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
