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
public class LoginEndpointTests : IAsyncDisposable
{
    private const string MensagemCredencialInvalida = "Usuário ou senha inválidos.";
    private const string MensagemUsuarioInativo = "Acesso bloqueado. Usuário inativo.";
    private const string MensagemUsuarioBloqueado =
        "Acesso temporariamente bloqueado por tentativas inválidas. Tente novamente em alguns minutos.";

    private static readonly Uri RotaLogin = new("/api/v1/auth/login", UriKind.Relative);
    private static readonly Uri RotaCriar = new("/api/v1/usuarios", UriKind.Relative);

    private readonly CarWashWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public LoginEndpointTests(PostgresFixture fixture)
    {
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    [Fact]
    public async Task POST_login_valido_retorna_200_com_accessToken()
    {
        var client = _factory.CreateClient();
        var (email, senha) = await CadastrarUsuarioAsync(client);

        var resp = await client.PostAsJsonAsync(RotaLogin, new { email, senha }, _json);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var corpo = await resp.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();
        corpo.GetProperty("expiresAt").GetDateTime().Should().BeAfter(DateTime.UtcNow);
        corpo.GetProperty("usuario").GetProperty("email").GetString().Should().Be(email);
        corpo.GetProperty("usuario").GetProperty("perfil").GetString().Should().Be("Funcionario");

        // Cache-Control: no-store presente.
        resp.Headers.CacheControl.Should().NotBeNull();
        resp.Headers.CacheControl!.NoStore.Should().BeTrue();
    }

    [Fact]
    public async Task POST_login_email_inexistente_retorna_401_mensagem_unificada()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync(RotaLogin, new
        {
            email = $"naoexiste-{Guid.NewGuid():N}@carwash.local",
            senha = "Senha1234",
        }, _json);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var corpo = await resp.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("title").GetString().Should().Be(MensagemCredencialInvalida);
        corpo.GetProperty("type").GetString().Should().Contain("invalid-credentials");
    }

    [Fact]
    public async Task POST_login_senha_errada_retorna_401_mesma_mensagem_que_email_inexistente()
    {
        var client = _factory.CreateClient();
        var (email, _) = await CadastrarUsuarioAsync(client);

        var resp = await client.PostAsJsonAsync(RotaLogin, new { email, senha = "OutraSenha9999" }, _json);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var corpo = await resp.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("title").GetString().Should().Be(MensagemCredencialInvalida);
    }

    [Fact]
    public async Task POST_login_usuario_inativo_com_senha_correta_retorna_403()
    {
        var client = _factory.CreateClient();
        var (email, senha) = await CadastrarUsuarioAsync(client);
        var id = await IdDoUsuarioPorEmailAsync(client, email, senha);

        var inativar = await client.PatchAsJsonAsync(
            new Uri($"/api/v1/usuarios/{id}/status", UriKind.Relative),
            new { ativo = false },
            _json);
        inativar.StatusCode.Should().Be(HttpStatusCode.OK);

        var resp = await client.PostAsJsonAsync(RotaLogin, new { email, senha }, _json);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var corpo = await resp.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("title").GetString().Should().Be(MensagemUsuarioInativo);
        corpo.GetProperty("type").GetString().Should().Contain("usuario-inativo");
    }

    [Fact]
    public async Task POST_login_email_uppercase_eh_normalizado_e_autentica()
    {
        var client = _factory.CreateClient();
        var (email, senha) = await CadastrarUsuarioAsync(client);

        var resp = await client.PostAsJsonAsync(RotaLogin, new { email = email.ToUpperInvariant(), senha }, _json);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await resp.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("usuario").GetProperty("email").GetString().Should().Be(email);
    }

    [Fact]
    public async Task POST_login_tres_falhas_consecutivas_bloqueiam_e_retornam_401_401_403()
    {
        var client = _factory.CreateClient();
        var (email, _) = await CadastrarUsuarioAsync(client);

        // 1ª e 2ª falha — 401, mensagem unificada.
        for (var i = 0; i < 2; i++)
        {
            var falha = await client.PostAsJsonAsync(RotaLogin, new { email, senha = "Errada9999" }, _json);
            falha.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            var corpoFalha = await falha.Content.ReadFromJsonAsync<JsonElement>(_json);
            corpoFalha.GetProperty("title").GetString().Should().Be(MensagemCredencialInvalida);
        }

        // 3ª falha — vira lockout, 403 com mensagem e bloqueadoAte preenchido.
        var bloqueio = await client.PostAsJsonAsync(RotaLogin, new { email, senha = "Errada9999" }, _json);
        bloqueio.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var corpoBloqueio = await bloqueio.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpoBloqueio.GetProperty("title").GetString().Should().Be(MensagemUsuarioBloqueado);
        corpoBloqueio.GetProperty("type").GetString().Should().Contain("usuario-bloqueado");
        corpoBloqueio.GetProperty("bloqueadoAte").GetDateTime().Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task POST_login_com_usuario_bloqueado_retorna_403_imediato_mesmo_com_senha_correta()
    {
        var client = _factory.CreateClient();
        var (email, senha) = await CadastrarUsuarioAsync(client);

        // Provoca o bloqueio (3 falhas).
        for (var i = 0; i < 3; i++)
        {
            await client.PostAsJsonAsync(RotaLogin, new { email, senha = "Errada9999" }, _json);
        }

        // Mesmo com senha correta, bloqueio ativo prevalece.
        var resp = await client.PostAsJsonAsync(RotaLogin, new { email, senha }, _json);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var corpo = await resp.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("title").GetString().Should().Be(MensagemUsuarioBloqueado);
        corpo.GetProperty("type").GetString().Should().Contain("usuario-bloqueado");
    }

    [Fact]
    public async Task POST_login_payload_vazio_retorna_400()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync(RotaLogin, new { }, _json);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<(string Email, string Senha)> CadastrarUsuarioAsync(HttpClient client)
    {
        var email = $"alice-{Guid.NewGuid():N}@carwash.local";
        const string senha = "Senha1234";
        var resp = await client.PostAsJsonAsync(RotaCriar, new
        {
            nome = "Alice",
            email,
            senha,
            perfil = "Funcionario",
        }, _json);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        return (email, senha);
    }

    private async Task<Guid> IdDoUsuarioPorEmailAsync(HttpClient client, string email, string senha)
    {
        // Reaproveita o login para descobrir o id (cenário "feliz" único).
        var resp = await client.PostAsJsonAsync(RotaLogin, new { email, senha }, _json);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await resp.Content.ReadFromJsonAsync<JsonElement>(_json);
        return corpo.GetProperty("usuario").GetProperty("id").GetGuid();
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
