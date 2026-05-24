using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarWash.Domain.Entities;
using CarWash.Infrastructure.Persistence;
using CarWash.IntegrationTests.Collections;
using CarWash.IntegrationTests.Fixtures;
using CarWash.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CarWash.IntegrationTests.Endpoints.Usuarios;

[Collection(nameof(PostgresCollection))]
public class CriarUsuarioEndpointTests : IAsyncDisposable
{
    private static readonly Uri RotaCriar = new("/api/v1/usuarios", UriKind.Relative);

    private readonly PostgresFixture _fixture;
    private readonly CarWashWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public CriarUsuarioEndpointTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    [Fact]
    public async Task POST_valido_retorna_201_com_Location_e_GET_retorna_200_sem_senha_hash()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var email = EmailUnico();

        var payload = new
        {
            nome = "Alice Silva",
            email,
            senha = "Senha1234",
            perfil = "Funcionario",
        };

        var response = await client.PostAsJsonAsync(RotaCriar, payload, _json);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.OriginalString.Should().StartWith("/api/v1/usuarios/");

        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("id").GetGuid().Should().NotBeEmpty();
        corpo.GetProperty("nome").GetString().Should().Be("Alice Silva");
        corpo.GetProperty("email").GetString().Should().Be(email);
        corpo.GetProperty("perfil").GetString().Should().Be("Funcionario");
        corpo.GetProperty("ativo").GetBoolean().Should().BeTrue();
        corpo.GetProperty("criadoEm").GetDateTime().Should().NotBe(default);
        corpo.GetProperty("atualizadoEm").GetDateTime().Should().NotBe(default);

        // Nunca expõe SenhaHash no response.
        corpo.TryGetProperty("senhaHash", out _).Should().BeFalse();
        corpo.TryGetProperty("senha", out _).Should().BeFalse();

        var id = corpo.GetProperty("id").GetGuid();

        // GET por id retorna o mesmo recurso.
        var getResp = await client.GetAsync(new Uri($"/api/v1/usuarios/{id}", UriKind.Relative));
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var getCorpo = await getResp.Content.ReadFromJsonAsync<JsonElement>(_json);
        getCorpo.GetProperty("id").GetGuid().Should().Be(id);
        getCorpo.GetProperty("email").GetString().Should().Be(email);
        getCorpo.TryGetProperty("senhaHash", out _).Should().BeFalse();

        // Linha persistida tem hash Argon2id.
        await using var db = CarWashDbContextFactoryForTests.Create(_fixture);
        var row = await db.Usuarios.AsNoTracking().FirstAsync(u => u.Id == id);
        row.SenhaHash.Should().StartWith("$argon2id$");
    }

    [Fact]
    public async Task POST_email_duplicado_case_insensitive_retorna_409()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var email = EmailUnico();

        var primeiro = await client.PostAsJsonAsync(RotaCriar, new
        {
            nome = "Primeiro",
            email,
            senha = "Senha1234",
            perfil = "Funcionario",
        }, _json);
        primeiro.StatusCode.Should().Be(HttpStatusCode.Created);

        var segundo = await client.PostAsJsonAsync(RotaCriar, new
        {
            nome = "Segundo",
            email = email.ToUpperInvariant(),
            senha = "Outra1234",
            perfil = "Admin",
        }, _json);

        segundo.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var corpo = await segundo.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("title").GetString()
            .Should().Be("Já existe usuário cadastrado com este e-mail.");
        corpo.GetProperty("type").GetString().Should().Contain("email-already-exists");
        corpo.GetProperty("correlationId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task POST_email_malformado_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var response = await client.PostAsJsonAsync(RotaCriar, new
        {
            nome = "Alice",
            email = "naoeemail",
            senha = "Senha1234",
            perfil = "Funcionario",
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("title").GetString()
            .Should().Be("Dados do usuário inválidos. Verifique os campos e tente novamente.");
        corpo.TryGetProperty("errors", out var erros).Should().BeTrue();
        erros.TryGetProperty("email", out _).Should().BeTrue();
    }

    [Fact]
    public async Task POST_senha_curta_retorna_400_com_mensagem_de_politica()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var response = await client.PostAsJsonAsync(RotaCriar, new
        {
            nome = "Alice",
            email = EmailUnico(),
            senha = "abc12",
            perfil = "Funcionario",
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.TryGetProperty("errors", out var erros).Should().BeTrue();
        erros.GetProperty("senha")[0].GetString().Should().Be("Senha não atende aos requisitos mínimos.");
    }

    [Fact]
    public async Task POST_perfil_invalido_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var response = await client.PostAsJsonAsync(RotaCriar, new
        {
            nome = "Alice",
            email = EmailUnico(),
            senha = "Senha1234",
            perfil = "Inexistente",
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_payload_vazio_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var response = await client.PostAsJsonAsync(RotaCriar, new { }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_grava_audit_log_UsuarioCadastrado_referenciando_o_id()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var email = EmailUnico();

        var response = await client.PostAsJsonAsync(RotaCriar, new
        {
            nome = "Carol",
            email,
            senha = "Senha1234",
            perfil = "Funcionario",
        }, _json);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        var id = corpo.GetProperty("id").GetGuid();

        await using var db = CarWashDbContextFactoryForTests.Create(_fixture);
        var log = await db.AuditLogs
            .AsNoTracking()
            .Where(a => a.Evento == "UsuarioCadastrado" && a.EntidadeId == id)
            .FirstOrDefaultAsync();

        log.Should().NotBeNull();
        log!.Entidade.Should().Be("Usuario");
        log.Dados.Should().NotBeNull();

        // Não deve vazar a senha em claro nem o hash legível — campos mascarados.
        log.Dados!.Should().NotContain("Senha1234");
        log.Dados.Should().Contain("***");
    }

    private static string EmailUnico() =>
        $"alice-{Guid.NewGuid():N}@carwash.local";

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
