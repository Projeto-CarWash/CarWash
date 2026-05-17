using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarWash.IntegrationTests.Collections;
using CarWash.IntegrationTests.Fixtures;
using CarWash.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CarWash.IntegrationTests.Endpoints.Auth;

/// <summary>
/// Fluxo end-to-end de login → refresh (com rotação) → logout (com revogação),
/// validando que:
/// <list type="bullet">
///   <item>O refresh token vem em <c>Set-Cookie</c> httpOnly (não no body do login).</item>
///   <item>O refresh rotaciona: a sessão anterior fica inválida após o uso.</item>
///   <item>O logout revoga a sessão e apaga o cookie.</item>
/// </list>
/// </summary>
[Collection(nameof(PostgresCollection))]
public class AuthFlowEndToEndTests : IAsyncDisposable
{
    private const string RefreshCookieName = "carwash_refresh_token";
    private static readonly Uri RotaLogin = new("/api/v1/auth/login", UriKind.Relative);
    private static readonly Uri RotaRefresh = new("/api/v1/auth/refresh", UriKind.Relative);
    private static readonly Uri RotaLogout = new("/api/v1/auth/logout", UriKind.Relative);
    private static readonly Uri RotaCriar = new("/api/v1/usuarios", UriKind.Relative);

    private readonly CarWashWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public AuthFlowEndToEndTests(PostgresFixture fixture)
    {
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    [Fact]
    public async Task Login_define_cookie_httpOnly_de_refresh_e_nao_devolve_refresh_no_body()
    {
        var client = ClienteSemCookies();
        var (email, senha) = await CadastrarAsync(client);

        var resp = await client.PostAsJsonAsync(RotaLogin, new { email, senha }, _json);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Body não deve conter o refresh.
        var corpo = await resp.Content.ReadAsStringAsync();
        corpo.Should().NotContain("refresh", "o body do login não pode incluir o refresh token");

        // Cookie deve estar presente e httpOnly.
        var setCookie = resp.Headers.GetValues("Set-Cookie").FirstOrDefault(c => c.StartsWith(RefreshCookieName, StringComparison.Ordinal));
        setCookie.Should().NotBeNull("login deve setar o cookie de refresh");
        setCookie!.Should().Contain("httponly", "cookie deve ser HttpOnly");
        setCookie.Should().Contain("samesite=strict", "cookie deve ser SameSite=Strict");
        setCookie.Should().Contain("path=/api/v1/auth", "cookie deve ser scoped para /api/v1/auth");
    }

    [Fact]
    public async Task Refresh_rotaciona_sessao_e_emite_novo_access()
    {
        var client = ClienteComCookies();
        var (email, senha) = await CadastrarAsync(client);

        var login = await client.PostAsJsonAsync(RotaLogin, new { email, senha }, _json);
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var accessAntigo = (await login.Content.ReadFromJsonAsync<JsonElement>(_json)).GetProperty("accessToken").GetString();

        var refresh = await client.PostAsync(RotaRefresh, content: null);
        refresh.StatusCode.Should().Be(HttpStatusCode.OK);

        var corpoRefresh = await refresh.Content.ReadFromJsonAsync<JsonElement>(_json);
        var accessNovo = corpoRefresh.GetProperty("accessToken").GetString();
        accessNovo.Should().NotBeNullOrWhiteSpace();
        accessNovo.Should().NotBe(accessAntigo, "rotação deve emitir novo access");

        // O cookie deve ter sido atualizado (novo Set-Cookie).
        refresh.Headers.GetValues("Set-Cookie").Should().Contain(c => c.StartsWith(RefreshCookieName, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Refresh_sem_cookie_retorna_401()
    {
        var client = ClienteSemCookies();
        var resp = await client.PostAsync(RotaRefresh, content: null);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_apos_rotacao_invalida_o_cookie_antigo()
    {
        // O cookie antigo, depois de usado, é revogado. Para reproduzir, vamos
        // capturar o valor do primeiro Set-Cookie e tentar usá-lo após um refresh.
        var client = ClienteSemCookies();
        var (email, senha) = await CadastrarAsync(client);

        var login = await client.PostAsJsonAsync(RotaLogin, new { email, senha }, _json);
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshCookie1 = ExtrairCookie(login, RefreshCookieName)
            ?? throw new InvalidOperationException("Login não definiu o cookie de refresh.");

        // Primeira renovação usando o cookie 1 — deve funcionar.
        var refresh1 = await EnviarRefreshComCookieAsync(refreshCookie1);
        refresh1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Segunda chamada com o MESMO cookie 1 — agora deve falhar (sessão revogada pela rotação).
        var refresh2 = await EnviarRefreshComCookieAsync(refreshCookie1);
        refresh2.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_revoga_sessao_e_apaga_cookie()
    {
        var client = ClienteComCookies();
        var (email, senha) = await CadastrarAsync(client);

        var login = await client.PostAsJsonAsync(RotaLogin, new { email, senha }, _json);
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        var logout = await client.PostAsync(RotaLogout, content: null);
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Cookie de "apagar" deve aparecer no Set-Cookie (Expires no passado).
        var setCookie = logout.Headers.GetValues("Set-Cookie").FirstOrDefault(c => c.StartsWith(RefreshCookieName, StringComparison.Ordinal));
        setCookie.Should().NotBeNull();
        setCookie!.Should().Contain("expires=", "logout deve sinalizar expiração do cookie");

        // Tentativa de refresh logo após o logout: cookie já foi enviado novo (vazio/expirado).
        // O HttpClient automaticamente "apaga" o cookie. Refresh fica sem cookie → 401.
        var refresh = await client.PostAsync(RotaRefresh, content: null);
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<HttpResponseMessage> EnviarRefreshComCookieAsync(string cookieValue)
    {
        // Cria um client "manual" sem cookie handler para controlar exatamente o que vai.
        var client = ClienteSemCookies();
        var req = new HttpRequestMessage(HttpMethod.Post, RotaRefresh);
        req.Headers.Add("Cookie", $"{RefreshCookieName}={cookieValue}");
        return await client.SendAsync(req);
    }

    private static string? ExtrairCookie(HttpResponseMessage response, string name)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return null;
        }

        foreach (var cookie in cookies)
        {
            var prefix = $"{name}=";
            if (cookie.StartsWith(prefix, StringComparison.Ordinal))
            {
                var rest = cookie.Substring(prefix.Length);
                var semi = rest.IndexOf(';');
                return semi < 0 ? rest : rest[..semi];
            }
        }

        return null;
    }

    private HttpClient ClienteSemCookies()
        => _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    private HttpClient ClienteComCookies()
        => _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

    private async Task<(string Email, string Senha)> CadastrarAsync(HttpClient client)
    {
        var email = $"flow-{Guid.NewGuid():N}@carwash.local";
        const string senha = "Senha1234";
        var resp = await client.PostAsJsonAsync(RotaCriar, new
        {
            nome = "Flow",
            email,
            senha,
            perfil = "Funcionario",
        }, _json);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        return (email, senha);
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
