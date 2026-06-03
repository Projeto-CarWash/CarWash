using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CarWash.Infrastructure.Persistence;
using CarWash.IntegrationTests.Collections;
using CarWash.IntegrationTests.Fixtures;
using CarWash.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CarWash.IntegrationTests.Endpoints.Filiais;

/// <summary>
/// RF018 — <c>PATCH /api/v1/filiais/{id}/celulas-ativas</c> ponta a ponta.
/// Cobre 200/400/401/404, consistência via GET, idempotência (sem auditoria
/// extra) e auditoria com {valorAnterior, valorNovo}. 403 por perfil: ver RF-FUT003.
/// </summary>
[Collection(nameof(PostgresCollection))]
public class AlterarCelulasAtivasEndpointTests : IAsyncDisposable
{
    private const string MensagemFaixa =
        "Valor de células ativas inválido. Informe um número inteiro entre 1 e 100.";

    private static readonly Uri RotaFiliais = new("/api/v1/filiais", UriKind.Relative);

    private readonly CarWashWebApplicationFactory _factory;
    private readonly PostgresFixture _fixture;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public AlterarCelulasAtivasEndpointTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    [Fact]
    public async Task PATCH_admin_valor_valido_retorna_200_e_GET_confirma()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var id = await CriarFilialAsync(client, celulas: 4);

        var patch = await client.PatchAsJsonAsync(RotaCelulas(id), new { celulasAtivas = 7 }, _json);

        patch.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await patch.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("id").GetGuid().Should().Be(id);
        corpo.GetProperty("celulasAtivas").GetInt32().Should().Be(7);

        // Consistência: GET subsequente devolve o valor atualizado.
        var get = await client.GetAsync(RotaPorId(id));
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var getCorpo = await get.Content.ReadFromJsonAsync<JsonElement>(_json);
        getCorpo.GetProperty("celulasAtivas").GetInt32().Should().Be(7);
    }

    [Fact]
    public async Task PATCH_mesmo_valor_duas_vezes_nao_cria_auditoria_adicional()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var id = await CriarFilialAsync(client, celulas: 4);

        // Primeira mudança real (4 → 9): gera 2 linhas (interceptor + logger).
        (await client.PatchAsJsonAsync(RotaCelulas(id), new { celulasAtivas = 9 }, _json))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        int aposPrimeira;
        await using (var db1 = CarWashDbContextFactoryForTests.Create(_fixture))
        {
            aposPrimeira = await db1.AuditLogs
                .AsNoTracking()
                .CountAsync(a => a.Evento == "FilialCelulasAlteradas" && a.EntidadeId == id);
        }

        aposPrimeira.Should().Be(2, "a mudança 4→9 grava 1 linha do interceptor + 1 do IAuditLogger");

        // Repetir o MESMO valor (9 → 9) é no-op idempotente: 200 mas sem novas linhas.
        (await client.PatchAsJsonAsync(RotaCelulas(id), new { celulasAtivas = 9 }, _json))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PatchAsJsonAsync(RotaCelulas(id), new { celulasAtivas = 9 }, _json))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        await using var db2 = CarWashDbContextFactoryForTests.Create(_fixture);
        int aposIdempotentes = await db2.AuditLogs
            .AsNoTracking()
            .CountAsync(a => a.Evento == "FilialCelulasAlteradas" && a.EntidadeId == id);

        aposIdempotentes.Should().Be(aposPrimeira, "PATCH idempotente não pode criar linhas de auditoria adicionais");
    }

    [Fact]
    public async Task PATCH_grava_auditoria_com_valorAnterior_e_valorNovo()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var id = await CriarFilialAsync(client, celulas: 4);

        (await client.PatchAsJsonAsync(RotaCelulas(id), new { celulasAtivas = 12 }, _json))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        await using var db = CarWashDbContextFactoryForTests.Create(_fixture);
        var logs = await db.AuditLogs
            .AsNoTracking()
            .Where(a => a.Evento == "FilialCelulasAlteradas" && a.EntidadeId == id)
            .ToListAsync();

        logs.Should().HaveCount(2);
        logs.Should().OnlyContain(l => l.Entidade == "Filial");
        logs.Should().OnlyContain(l => !string.IsNullOrWhiteSpace(l.CorrelationId));
        logs.Should().OnlyContain(l => l.UsuarioId != null);

        // A linha explícita do IAuditLogger carrega {valorAnterior, valorNovo}.
        logs.Should().Contain(l =>
            l.Dados != null
            && l.Dados.Contains("valorAnterior", StringComparison.Ordinal)
            && l.Dados.Contains("valorNovo", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task PATCH_fora_da_faixa_retorna_400_com_mensagem_exata(int celulas)
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var id = await CriarFilialAsync(client, celulas: 4);

        var patch = await client.PatchAsJsonAsync(RotaCelulas(id), new { celulasAtivas = celulas }, _json);

        patch.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await patch.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("errors").GetProperty("celulasAtivas")[0].GetString()
            .Should().Be(MensagemFaixa);
    }

    [Theory]
    [InlineData("2.5")]
    [InlineData("\"dez\"")]
    [InlineData("null")]
    public async Task PATCH_tipo_invalido_ou_null_retorna_400(string valorJson)
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var id = await CriarFilialAsync(client, celulas: 4);

        string body = $"{{\"celulasAtivas\":{valorJson}}}";
        var req = new HttpRequestMessage(HttpMethod.Patch, RotaCelulas(id))
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        var patch = await client.SendAsync(req);

        patch.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PATCH_filial_inexistente_retorna_404_com_mensagem_exata()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var patch = await client.PatchAsJsonAsync(RotaCelulas(Guid.NewGuid()), new { celulasAtivas = 7 }, _json);

        patch.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var corpo = await patch.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("title").GetString().Should().Be("Filial não encontrada.");
        corpo.GetProperty("type").GetString().Should().Contain("not-found");
    }

    [Fact]
    public async Task PATCH_sem_token_retorna_401()
    {
        var anonimo = _factory.CreateClient();

        var patch = await anonimo.PatchAsJsonAsync(RotaCelulas(Guid.NewGuid()), new { celulasAtivas = 7 }, _json);

        // Sem JwtBearerEvents customizado (reconciliação com development): o 401 volta
        // ao default do framework — corpo não traz mais `title` customizado. Asserta
        // apenas o status.
        patch.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // 403 por perfil: ver RF-FUT003. A policy Admin e os JwtBearerEvents foram
    // removidos na reconciliação com a development (RequireAuthorization() puro),
    // logo não há mais 403 por perfil — funcionário autenticado consegue alterar.
    private async Task<Guid> CriarFilialAsync(HttpClient adminClient, int celulas)
    {
        string nome = $"Filial {Guid.NewGuid():N}"[..30];
        string codigo = CodigoUnico();
        var response = await adminClient.PostAsJsonAsync(RotaFiliais, new { nome, codigo, celulasAtivas = celulas }, _json);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>(_json)).GetProperty("id").GetGuid();
    }

    /// <summary>
    /// Código alfanumérico maiúsculo único por filial (regex ^[A-Z0-9]{2,20}$). O
    /// POST de filial passou a exigir `codigo` após a reconciliação com a development.
    /// </summary>
    private static string CodigoUnico() => $"F{Guid.NewGuid():N}"[..8].ToUpperInvariant();

    private static Uri RotaCelulas(Guid id) =>
        new($"/api/v1/filiais/{id}/celulas-ativas", UriKind.Relative);

    private static Uri RotaPorId(Guid id) =>
        new($"/api/v1/filiais/{id}", UriKind.Relative);

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
