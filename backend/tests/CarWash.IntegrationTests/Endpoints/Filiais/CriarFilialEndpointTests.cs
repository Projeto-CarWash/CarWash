using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CarWash.Application.Filiais.CriarFilial;
using CarWash.Infrastructure.Persistence;
using CarWash.IntegrationTests.Collections;
using CarWash.IntegrationTests.Fixtures;
using CarWash.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CarWash.IntegrationTests.Endpoints.Filiais;

/// <summary>
/// RF018 — <c>POST /api/v1/filiais</c> ponta a ponta com Testcontainers + PostgreSQL
/// real. Cobre 201/400/401/403/409 com mensagens e slugs EXATOS do card, mais a
/// auditoria dupla (interceptor + IAuditLogger).
/// </summary>
[Collection(nameof(PostgresCollection))]
public class CriarFilialEndpointTests : IAsyncDisposable
{
    private const string MensagemFaixa =
        "Valor de células ativas inválido. Informe um número inteiro entre 1 e 100.";

    private static readonly Uri RotaFiliais = new("/api/v1/filiais", UriKind.Relative);

    private readonly CarWashWebApplicationFactory _factory;
    private readonly PostgresFixture _fixture;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public CriarFilialEndpointTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    [Fact]
    public async Task POST_admin_payload_valido_retorna_201_com_location_e_camelCase()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var response = await client.PostAsJsonAsync(RotaFiliais, new
        {
            nome = NomeUnico(),
            celulasAtivas = 4,
            timezone = "America/Sao_Paulo",
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.OriginalString.Should().StartWith("/api/v1/filiais/");

        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("id").GetGuid().Should().NotBeEmpty();
        corpo.GetProperty("celulasAtivas").GetInt32().Should().Be(4);
        corpo.GetProperty("timezone").GetString().Should().Be("America/Sao_Paulo");
        corpo.GetProperty("ativa").GetBoolean().Should().BeTrue();
        corpo.GetProperty("criadoEm").GetDateTime().Should().NotBe(default);
        corpo.GetProperty("atualizadoEm").GetDateTime().Should().NotBe(default);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public async Task POST_admin_celulas_no_limite_retorna_201(int celulas)
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var response = await client.PostAsJsonAsync(RotaFiliais, new
        {
            nome = NomeUnico(),
            celulasAtivas = celulas,
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("celulasAtivas").GetInt32().Should().Be(celulas);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task POST_admin_celulas_fora_da_faixa_retorna_400_com_mensagem_exata(int celulas)
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var response = await client.PostAsJsonAsync(RotaFiliais, new
        {
            nome = NomeUnico(),
            celulasAtivas = celulas,
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("errors").GetProperty("celulasAtivas")[0].GetString()
            .Should().Be(MensagemFaixa);
    }

    [Theory]
    [InlineData("2.5")]
    [InlineData("\"dez\"")]
    public async Task POST_admin_celulas_tipo_invalido_retorna_400(string valorJson)
    {
        // Tipo não-inteiro em `celulasAtivas` (decimal/string) falha na desserialização
        // do `[FromBody] CriarFilialCommand` (System.Text.Json → int?). Para o POST, o
        // RequestDelegateFactory curto-circuita com 400 de corpo vazio ANTES do
        // ValidationFilter e sem lançar BadHttpRequestException capturável — contrato:
        // o valor inválido é rejeitado com 400.
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var nome = NomeUnico();
        var body = $"{{\"nome\":\"{nome}\",\"celulasAtivas\":{valorJson}}}";

        var response = await client.PostAsync(
            RotaFiliais,
            new StringContent(body, Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_admin_celulas_null_retorna_400_obrigatorio()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var response = await client.PostAsJsonAsync(RotaFiliais, new
        {
            nome = NomeUnico(),
            celulasAtivas = (int?)null,
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("errors").GetProperty("celulasAtivas")[0].GetString()
            .Should().Be(CriarFilialCommandValidator.MensagemCelulasObrigatorio);
    }

    [Fact]
    public async Task POST_admin_body_vazio_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var response = await client.PostAsJsonAsync(RotaFiliais, new { }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.TryGetProperty("errors", out _).Should().BeTrue();
    }

    [Fact]
    public async Task POST_admin_nome_duplicado_retorna_409_com_mensagem_e_slug_exatos()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var nome = NomeUnico();

        var primeiro = await client.PostAsJsonAsync(RotaFiliais, new { nome, celulasAtivas = 4 }, _json);
        primeiro.StatusCode.Should().Be(HttpStatusCode.Created);

        var segundo = await client.PostAsJsonAsync(RotaFiliais, new { nome, celulasAtivas = 10 }, _json);

        segundo.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var corpo = await segundo.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("title").GetString()
            .Should().Be("Já existe uma filial cadastrada com este nome.");
        corpo.GetProperty("type").GetString().Should().Contain("filial-nome-duplicado");
        corpo.GetProperty("correlationId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task POST_sem_token_retorna_401_com_mensagem_slug_e_content_type()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(RotaFiliais, new { nome = NomeUnico(), celulasAtivas = 4 }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("title").GetString()
            .Should().Be("Autenticação obrigatória para executar esta operação.");
        corpo.GetProperty("type").GetString().Should().Contain("auth-required");
        corpo.GetProperty("correlationId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task POST_funcionario_retorna_403_com_mensagem_slug_e_content_type()
    {
        var client = await FuncionarioHttpClient.CreateAsync(_factory);

        var response = await client.PostAsJsonAsync(RotaFiliais, new { nome = NomeUnico(), celulasAtivas = 4 }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("title").GetString()
            .Should().Be("Você não possui permissão para alterar configuração da filial.");
        corpo.GetProperty("type").GetString().Should().Contain("forbidden");
    }

    [Fact]
    public async Task POST_grava_duas_linhas_de_auditoria_para_a_filial_criada()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var response = await client.PostAsJsonAsync(RotaFiliais, new
        {
            nome = NomeUnico(),
            celulasAtivas = 6,
            timezone = "America/Sao_Paulo",
        }, _json);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = (await response.Content.ReadFromJsonAsync<JsonElement>(_json)).GetProperty("id").GetGuid();

        await using var db = CarWashDbContextFactoryForTests.Create(_fixture);
        var logs = await db.AuditLogs
            .AsNoTracking()
            .Where(a => a.Evento == "FilialCriada" && a.EntidadeId == id)
            .ToListAsync();

        // 2 linhas: 1 do AuditLogInterceptor (snapshot do INSERT) + 1 do IAuditLogger.
        logs.Should().HaveCount(2);
        logs.Should().OnlyContain(l => l.Entidade == "Filial");
        logs.Should().OnlyContain(l => !string.IsNullOrWhiteSpace(l.CorrelationId));

        // A linha do IAuditLogger carrega os dados específicos {Nome, CelulasAtivas, Timezone}
        // — serializados a partir do anonymous object do handler (PascalCase), sem o
        // wrapper {state, snapshot} que o interceptor adiciona.
        logs.Should().Contain(l =>
            l.Dados != null
            && l.Dados.Contains("\"CelulasAtivas\"", StringComparison.Ordinal)
            && l.Dados.Contains("\"Timezone\"", StringComparison.Ordinal)
            && !l.Dados.Contains("snapshot", StringComparison.Ordinal));
    }

    private static string NomeUnico() => $"Filial {Guid.NewGuid():N}"[..30];

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
