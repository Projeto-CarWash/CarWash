using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarWash.Application.Auth.Login;
using CarWash.Infrastructure.Persistence;
using CarWash.IntegrationTests.Collections;
using CarWash.IntegrationTests.Fixtures;
using CarWash.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CarWash.IntegrationTests.Endpoints.Auth;

/// <summary>
/// Cobertura RNF003 / RF001 — bloqueio temporário após N tentativas inválidas.
/// Cada teste mapeia a um CA do card-203:
/// <list type="bullet">
///   <item>CA-203.1 — lockout após N falhas → 403.</item>
///   <item>CA-203.2 — login durante lockout → 403 mesmo com senha correta.</item>
///   <item>CA-203.3 — anti-enumeration (mensagem unificada com e-mail inexistente).</item>
///   <item>CA-203.4 — liberação após <c>DuracaoBloqueio</c> (manipulação direta de
///     <c>BloqueadoAte</c> via repositório/DbContext — L1 opção 2).</item>
///   <item>CA-203.5 — sucesso zera contador de tentativas inválidas.</item>
///   <item>CA-203.6 — auditoria do bloqueio (`EventoUsuarioBloqueado` na audit table).</item>
/// </list>
/// </summary>
[Collection(nameof(PostgresCollection))]
public class AuthLockoutTests : IAsyncDisposable
{
    private const string MensagemCredencialInvalida = "Usuário ou senha inválidos.";
    private const string MensagemUsuarioBloqueado =
        "Acesso temporariamente bloqueado por tentativas inválidas. Tente novamente em alguns minutos.";

    private static readonly Uri RotaLogin = new("/api/v1/auth/login", UriKind.Relative);
    private static readonly Uri RotaCriarUsuario = new("/api/v1/usuarios", UriKind.Relative);

    private readonly CarWashWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public AuthLockoutTests(PostgresFixture fixture)
    {
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    [Fact(Skip = "lockout desativado temporariamente — card-134")]
    public async Task CA203_1_Lockout_apos_N_tentativas_invalidas_retorna_403()
    {
        var client = _factory.CreateClient();
        var (email, _) = await CadastrarUsuarioAsync();

        for (int i = 0; i < LoginHandler.LimiteTentativasInvalidas - 1; i++)
        {
            var falha = await client.PostAsJsonAsync(RotaLogin, new { email, senha = "Errada9999" }, _json);
            falha.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        var bloqueio = await client.PostAsJsonAsync(RotaLogin, new { email, senha = "Errada9999" }, _json);
        bloqueio.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var corpo = await bloqueio.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("title").GetString().Should().Be(MensagemUsuarioBloqueado);
        corpo.GetProperty("type").GetString().Should().Contain("usuario-bloqueado");
        corpo.GetProperty("bloqueadoAte").GetDateTime().Should().BeAfter(DateTime.UtcNow);
    }

    [Fact(Skip = "lockout desativado temporariamente — card-134")]
    public async Task CA203_2_Login_durante_lockout_retorna_403_mesmo_com_senha_correta()
    {
        var client = _factory.CreateClient();
        var (email, senha) = await CadastrarUsuarioAsync();

        for (int i = 0; i < LoginHandler.LimiteTentativasInvalidas; i++)
        {
            await client.PostAsJsonAsync(RotaLogin, new { email, senha = "Errada9999" }, _json);
        }

        var resp = await client.PostAsJsonAsync(RotaLogin, new { email, senha }, _json);

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var corpo = await resp.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("title").GetString().Should().Be(MensagemUsuarioBloqueado);
    }

    [Fact(Skip = "lockout desativado temporariamente — card-134")]
    public async Task CA203_3_Email_inexistente_retorna_mesma_mensagem_que_credencial_invalida()
    {
        var client = _factory.CreateClient();
        var (emailExistente, _) = await CadastrarUsuarioAsync();

        var respSenhaErrada = await client.PostAsJsonAsync(
            RotaLogin,
            new { email = emailExistente, senha = "Errada9999" },
            _json);
        var respEmailInexistente = await client.PostAsJsonAsync(
            RotaLogin,
            new { email = $"fantasma-{Guid.NewGuid():N}@carwash.local", senha = "Errada9999" },
            _json);

        respSenhaErrada.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        respEmailInexistente.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var corpoSenhaErrada = await respSenhaErrada.Content.ReadFromJsonAsync<JsonElement>(_json);
        var corpoEmailInexistente = await respEmailInexistente.Content.ReadFromJsonAsync<JsonElement>(_json);

        corpoSenhaErrada.GetProperty("title").GetString().Should().Be(MensagemCredencialInvalida);
        corpoEmailInexistente.GetProperty("title").GetString().Should().Be(MensagemCredencialInvalida);
    }

    [Fact(Skip = "lockout desativado temporariamente — card-134")]
    public async Task CA203_4_Lockout_expira_apos_duracao_bloqueio_e_login_correto_volta_a_200()
    {
        var client = _factory.CreateClient();
        var (email, senha) = await CadastrarUsuarioAsync();

        // Provoca o bloqueio.
        for (int i = 0; i < LoginHandler.LimiteTentativasInvalidas; i++)
        {
            await client.PostAsJsonAsync(RotaLogin, new { email, senha = "Errada9999" }, _json);
        }

        // Manipulação direta de BloqueadoAte para uma data no passado — simula a
        // expiração da janela DuracaoBloqueio sem refator de IClock (decisão do
        // arquiteto registrada no card 203 — L1 opção 2).
        await AvancarRelogioAsync(email);

        var resp = await client.PostAsJsonAsync(RotaLogin, new { email, senha }, _json);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await resp.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact(Skip = "lockout desativado temporariamente — card-134")]
    public async Task CA203_5_Sucesso_zera_contador_de_tentativas_invalidas()
    {
        var client = _factory.CreateClient();
        var (email, senha) = await CadastrarUsuarioAsync();

        // Duas falhas seguidas (abaixo do limite).
        for (int i = 0; i < 2; i++)
        {
            var falha = await client.PostAsJsonAsync(RotaLogin, new { email, senha = "Errada9999" }, _json);
            falha.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // Login correto zera o contador.
        var sucesso = await client.PostAsJsonAsync(RotaLogin, new { email, senha }, _json);
        sucesso.StatusCode.Should().Be(HttpStatusCode.OK);

        // Nova rodada: limite-1 falhas devem continuar voltando 401.
        for (int i = 0; i < LoginHandler.LimiteTentativasInvalidas - 1; i++)
        {
            var falha = await client.PostAsJsonAsync(RotaLogin, new { email, senha = "Errada9999" }, _json);
            falha.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // E só a N-ésima reativa o lockout.
        var bloqueio = await client.PostAsJsonAsync(RotaLogin, new { email, senha = "Errada9999" }, _json);
        bloqueio.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(Skip = "lockout desativado temporariamente — card-134")]
    public async Task CA203_6_Bloqueio_registra_evento_de_auditoria()
    {
        var client = _factory.CreateClient();
        var (email, _) = await CadastrarUsuarioAsync();

        for (int i = 0; i < LoginHandler.LimiteTentativasInvalidas; i++)
        {
            await client.PostAsJsonAsync(RotaLogin, new { email, senha = "Errada9999" }, _json);
        }

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CarWashDbContext>();
        var usuarioId = await db.Usuarios
            .Where(u => u.EmailValor == email)
            .Select(u => u.Id)
            .FirstAsync();

        bool possuiEventoBloqueado = await db.AuditLogs
            .AnyAsync(a => a.Evento == LoginHandler.EventoUsuarioBloqueado && a.EntidadeId == usuarioId);

        possuiEventoBloqueado.Should().BeTrue(
            "o LoginHandler deve registrar EventoUsuarioBloqueado quando a N-ésima falha ativa o lockout (RNF009).");
    }

    private async Task AvancarRelogioAsync(string email)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CarWashDbContext>();
        await db.Usuarios
            .Where(u => u.EmailValor == email)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.BloqueadoAte, DateTime.UtcNow.AddMinutes(-1)));
    }

    private async Task<(string Email, string Senha)> CadastrarUsuarioAsync()
    {
        string email = $"lockout-{Guid.NewGuid():N}@carwash.local";
        const string senha = "Senha1234";

        using var autenticado = await AuthenticatedHttpClient.CreateAsync(_factory);
        var resp = await autenticado.PostAsJsonAsync(RotaCriarUsuario, new
        {
            nome = "Alice Lockout",
            email,
            senha,
            perfil = "Funcionario",
        }, _json);

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        return (email, senha);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
