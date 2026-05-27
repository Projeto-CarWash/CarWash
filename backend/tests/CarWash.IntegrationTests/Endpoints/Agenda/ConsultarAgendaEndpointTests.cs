using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarWash.Domain.Entities;
using CarWash.Domain.ValueObjects;
using CarWash.Infrastructure.Persistence;
using CarWash.IntegrationTests.Collections;
using CarWash.IntegrationTests.Fixtures;
using CarWash.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CarWash.IntegrationTests.Endpoints.Agenda;

/// <summary>
/// Cobre o RF009 (consulta de agenda — card 132) ponta a ponta: formatos simples
/// e detalhado, validações de período/formato/filtros, autorização, consistência
/// entre formatos e ordenação. Testcontainers + PostgreSQL real + WebApplicationFactory.
/// </summary>
[Collection(nameof(PostgresCollection))]
public class ConsultarAgendaEndpointTests : IAsyncDisposable
{
    /// <summary>Id do admin semeado pela migration InitialSchema (usado como criador/responsável).</summary>
    private static readonly Guid AdminId = new("00000000-0000-0000-0000-000000000001");

    private readonly CarWashWebApplicationFactory _factory;
    private readonly PostgresFixture _fixture;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public ConsultarAgendaEndpointTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    // ---------------------------------------------------------------------
    // Item 1 — Consulta simples com período válido: 200 + 8 campos de resumo.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task GET_simples_periodo_valido_retorna_200_com_8_campos_de_resumo()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var cenario = await SemearAgendamentoAsync(servicos: 2);

        var response = await client.GetAsync(MontarUrl("simples", cenario));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("message").GetString().Should().Be("Agenda consultada com sucesso.");
        corpo.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();

        var data = corpo.GetProperty("data");
        data.GetArrayLength().Should().Be(1);

        var item = data[0];
        string[] campos = item.EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToArray();
        campos.Should().BeEquivalentTo(
            "agendamentoId", "inicio", "fim", "titulo", "status",
            "clienteNome", "veiculoPlaca", "servicosResumo");

        item.GetProperty("agendamentoId").GetGuid().Should().Be(cenario.AgendamentoId);
        item.GetProperty("status").GetString().Should().Be("AGENDADO");
        item.GetProperty("clienteNome").GetString().Should().Be(cenario.ClienteNome);
        item.GetProperty("veiculoPlaca").GetString().Should().Be(cenario.VeiculoPlaca);

        // titulo = nome do 1o servico; servicosResumo = "<1o> + <N-1>" para N>1.
        item.GetProperty("titulo").GetString().Should().Be(cenario.ServicoNomes[0]);
        item.GetProperty("servicosResumo").GetString().Should().Be($"{cenario.ServicoNomes[0]} + 1");
    }

    // ---------------------------------------------------------------------
    // Item 2 — Consulta detalhada com período válido: 200 + campos completos.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task GET_detalhado_periodo_valido_retorna_200_com_campos_completos()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var cenario = await SemearAgendamentoAsync(servicos: 2);

        var response = await client.GetAsync(MontarUrl("detalhado", cenario));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        var item = corpo.GetProperty("data")[0];

        string[] campos = item.EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToArray();
        campos.Should().BeEquivalentTo(
            "agendamentoId", "status", "filialId", "inicio", "fim",
            "duracaoTotalMin", "valorTotal", "cliente", "veiculo", "servicos",
            "observacoes", "criadoEm", "atualizadoEm");

        item.GetProperty("agendamentoId").GetGuid().Should().Be(cenario.AgendamentoId);
        item.GetProperty("status").GetString().Should().Be("AGENDADO");
        item.GetProperty("filialId").GetGuid().Should().Be(cenario.FilialId);
        item.GetProperty("duracaoTotalMin").GetInt32().Should().Be(cenario.DuracaoTotalMin);
        item.GetProperty("valorTotal").GetDecimal().Should().Be(cenario.ValorTotal);
        item.GetProperty("observacoes").GetString().Should().Be(cenario.Observacoes);

        var cliente = item.GetProperty("cliente");
        cliente.GetProperty("id").GetGuid().Should().Be(cenario.ClienteId);
        cliente.GetProperty("nome").GetString().Should().Be(cenario.ClienteNome);
        cliente.GetProperty("cpfCnpj").GetString().Should().Be(cenario.ClienteCpf);
        cliente.GetProperty("celular").GetString().Should().Be(cenario.ClienteCelular);

        var veiculo = item.GetProperty("veiculo");
        veiculo.GetProperty("id").GetGuid().Should().Be(cenario.VeiculoId);
        veiculo.GetProperty("placa").GetString().Should().Be(cenario.VeiculoPlaca);
        veiculo.GetProperty("modelo").GetString().Should().Be("Civic");
        veiculo.GetProperty("fabricante").GetString().Should().Be("Honda");
        veiculo.GetProperty("cor").GetString().Should().Be("Preto");

        var servicos = item.GetProperty("servicos");
        servicos.GetArrayLength().Should().Be(2);

        // duracaoMin/preco vem do snapshot do AgendamentoItem (RN006), nao do catalogo.
        var servico0 = servicos[0];
        servico0.GetProperty("nome").GetString().Should().Be(cenario.ServicoNomes[0]);
        servico0.GetProperty("duracaoMin").GetInt32().Should().Be(cenario.ServicoDuracoes[0]);
        servico0.GetProperty("preco").GetDecimal().Should().Be(cenario.ServicoPrecos[0]);
    }

    // ---------------------------------------------------------------------
    // Item 3 — Mesmo agendamentoId: consistencia entre simples e detalhado.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task GET_mesmo_agendamento_mantem_consistencia_entre_simples_e_detalhado()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var cenario = await SemearAgendamentoAsync(servicos: 2);

        var respostaSimples = await client.GetAsync(MontarUrl("simples", cenario));
        var respostaDetalhada = await client.GetAsync(MontarUrl("detalhado", cenario));

        respostaSimples.StatusCode.Should().Be(HttpStatusCode.OK);
        respostaDetalhada.StatusCode.Should().Be(HttpStatusCode.OK);

        var simples = (await respostaSimples.Content.ReadFromJsonAsync<JsonElement>(_json))
            .GetProperty("data")[0];
        var detalhado = (await respostaDetalhada.Content.ReadFromJsonAsync<JsonElement>(_json))
            .GetProperty("data")[0];

        // Mesmo agendamento — campos-base devem ser idênticos entre os dois formatos.
        simples.GetProperty("agendamentoId").GetGuid()
            .Should().Be(detalhado.GetProperty("agendamentoId").GetGuid());
        simples.GetProperty("inicio").GetDateTime()
            .Should().Be(detalhado.GetProperty("inicio").GetDateTime());
        simples.GetProperty("fim").GetDateTime()
            .Should().Be(detalhado.GetProperty("fim").GetDateTime());
        simples.GetProperty("status").GetString()
            .Should().Be(detalhado.GetProperty("status").GetString());
        simples.GetProperty("clienteNome").GetString()
            .Should().Be(detalhado.GetProperty("cliente").GetProperty("nome").GetString());
        simples.GetProperty("veiculoPlaca").GetString()
            .Should().Be(detalhado.GetProperty("veiculo").GetProperty("placa").GetString());
    }

    // ---------------------------------------------------------------------
    // Item 4 — Consulta sem eventos: 200 com data: [] e mensagem correta.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task GET_periodo_sem_eventos_retorna_200_com_lista_vazia_e_mensagem()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var filialId = await SemearFilialVaziaAsync();

        var url = MontarUrl(
            "simples",
            filialId,
            inicio: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            fim: new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc));

        var response = await client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("data").GetArrayLength().Should().Be(0);
        corpo.GetProperty("message").GetString()
            .Should().Be("Nenhum evento encontrado para o período selecionado.");
    }

    // ---------------------------------------------------------------------
    // Item 5 — inicio >= fim: 400.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task GET_inicio_igual_fim_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var instante = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);

        var url = MontarUrl("simples", Guid.NewGuid(), inicio: instante, fim: instante);
        var response = await client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GarantirEnvelopeDeErroAsync(response);
    }

    [Fact]
    public async Task GET_inicio_posterior_ao_fim_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var url = MontarUrl(
            "simples",
            Guid.NewGuid(),
            inicio: new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc),
            fim: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        var response = await client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---------------------------------------------------------------------
    // Item 6 — Periodo > 31 dias: 400. Limite (exatamente 31 dias) passa.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task GET_periodo_acima_de_31_dias_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var inicio = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        var url = MontarUrl("simples", Guid.NewGuid(), inicio: inicio, fim: inicio.AddDays(32));
        var response = await client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_periodo_exatamente_31_dias_retorna_200()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var filialId = await SemearFilialVaziaAsync();
        var inicio = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        // Limite-fronteira: janela == 31 dias deve passar (boundary).
        var url = MontarUrl("simples", filialId, inicio: inicio, fim: inicio.AddDays(31));
        var response = await client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ---------------------------------------------------------------------
    // Item 7 — formato invalido ou ausente: 400.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task GET_formato_invalido_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var inicio = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        var url = MontarUrl("resumido", Guid.NewGuid(), inicio: inicio, fim: inicio.AddDays(1));
        var response = await client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_formato_ausente_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var inicio = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        var url = new Uri(
            $"/api/v1/agenda?inicio={Iso(inicio)}&fim={Iso(inicio.AddDays(1))}&filialId={Guid.NewGuid()}",
            UriKind.Relative);
        var response = await client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_filial_ausente_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var inicio = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        var url = new Uri(
            $"/api/v1/agenda?formato=simples&inicio={Iso(inicio)}&fim={Iso(inicio.AddDays(1))}",
            UriKind.Relative);
        var response = await client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_inicio_em_formato_nao_iso_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var url = new Uri(
            $"/api/v1/agenda?formato=simples&inicio=01-06-2026&fim=2026-06-02T00:00:00Z&filialId={Guid.NewGuid()}",
            UriKind.Relative);
        var response = await client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---------------------------------------------------------------------
    // Item 8 — Filtros opcionais invalidos: 400.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task GET_clienteId_nao_uuid_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var inicio = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        var url = new Uri(
            $"/api/v1/agenda?formato=simples&inicio={Iso(inicio)}&fim={Iso(inicio.AddDays(1))}"
            + $"&filialId={Guid.NewGuid()}&clienteId=nao-e-uuid",
            UriKind.Relative);
        var response = await client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_usuarioId_nao_uuid_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var inicio = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        var url = new Uri(
            $"/api/v1/agenda?formato=simples&inicio={Iso(inicio)}&fim={Iso(inicio.AddDays(1))}"
            + $"&filialId={Guid.NewGuid()}&usuarioId=123",
            UriKind.Relative);
        var response = await client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_status_fora_dos_quatro_valores_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var inicio = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        var url = new Uri(
            $"/api/v1/agenda?formato=simples&inicio={Iso(inicio)}&fim={Iso(inicio.AddDays(1))}"
            + $"&filialId={Guid.NewGuid()}&status=PENDENTE",
            UriKind.Relative);
        var response = await client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---------------------------------------------------------------------
    // Item 9 — Sem autenticacao: 401.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task GET_sem_token_retorna_401()
    {
        var client = _factory.CreateClient();
        var inicio = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        var url = MontarUrl("simples", Guid.NewGuid(), inicio: inicio, fim: inicio.AddDays(1));
        var response = await client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------------------------------------------------------------------
    // Item 11 — Erro interno: 500 sem expor detalhes tecnicos.
    // O endpoint nao tem caminho que dispare 500 deterministicamente; este teste
    // guarda contra regressao de vazamento (qualquer 5xx deve trazer corpo neutro).
    // ---------------------------------------------------------------------
    [Fact]
    public async Task GET_qualquer_5xx_nao_expoe_stack_trace()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var cenario = await SemearAgendamentoAsync(servicos: 1);

        var response = await client.GetAsync(MontarUrl("detalhado", cenario));

        if ((int)response.StatusCode >= 500)
        {
            string corpo = await response.Content.ReadAsStringAsync();
            corpo.Should().NotContain("at CarWash.");
            corpo.Should().NotContain("Exception:");
            corpo.Should().NotContain("StackTrace");
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    // ---------------------------------------------------------------------
    // Extra — ordenacao padrao: inicio ASC, depois criadoEm ASC.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task GET_resultado_ordenado_por_inicio_asc()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var filialId = await SemearFilialVaziaAsync();
        var (clienteId, veiculoId) = await SemearClienteVeiculoAsync();

        // Insere fora de ordem: o mais tardio primeiro.
        var baseInicio = new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc);
        var idTarde = await SemearAgendamentoCruAsync(filialId, clienteId, veiculoId, baseInicio.AddHours(4));
        var idCedo = await SemearAgendamentoCruAsync(filialId, clienteId, veiculoId, baseInicio);
        var idMeio = await SemearAgendamentoCruAsync(filialId, clienteId, veiculoId, baseInicio.AddHours(2));

        var url = MontarUrl(
            "simples",
            filialId,
            inicio: baseInicio.AddHours(-1),
            fim: baseInicio.AddHours(6));
        var response = await client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = (await response.Content.ReadFromJsonAsync<JsonElement>(_json)).GetProperty("data");

        var ids = data.EnumerateArray()
            .Select(e => e.GetProperty("agendamentoId").GetGuid())
            .ToArray();
        ids.Should().Equal(idCedo, idMeio, idTarde);

        var inicios = data.EnumerateArray()
            .Select(e => e.GetProperty("inicio").GetDateTime())
            .ToArray();
        inicios.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GET_mesmo_inicio_desempata_por_criadoEm_asc()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var filialId = await SemearFilialVaziaAsync();
        var (clienteId, veiculoId) = await SemearClienteVeiculoAsync();
        var (clienteId2, veiculoId2) = await SemearClienteVeiculoAsync();

        // Dois agendamentos com MESMO inicio; SaveChanges separados + um pequeno
        // gap real garantem criadoEm estritamente distintos (o interceptor de
        // auditoria carimba CriadoEm com DateTime.UtcNow na inserção). O delay
        // torna o tie-break determinístico — não é espera arbitrária de timing.
        var instante = new DateTime(2026, 7, 15, 13, 0, 0, DateTimeKind.Utc);
        var primeiroCriado = await SemearAgendamentoCruAsync(filialId, clienteId, veiculoId, instante);
        await Task.Delay(TimeSpan.FromMilliseconds(50));
        var segundoCriado = await SemearAgendamentoCruAsync(filialId, clienteId2, veiculoId2, instante);

        var url = MontarUrl(
            "simples",
            filialId,
            inicio: instante.AddHours(-1),
            fim: instante.AddHours(1));
        var response = await client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = (await response.Content.ReadFromJsonAsync<JsonElement>(_json)).GetProperty("data");

        var ids = data.EnumerateArray()
            .Select(e => e.GetProperty("agendamentoId").GetGuid())
            .ToArray();
        ids.Should().Equal(primeiroCriado, segundoCriado);
    }

    // ---------------------------------------------------------------------
    // Extra — EM_ANDAMENTO: filtro valido (nao 400) que resolve para data: [].
    // ---------------------------------------------------------------------
    [Fact]
    public async Task GET_status_em_andamento_retorna_200_com_lista_vazia()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        // Mesmo havendo agendamentos no periodo, EM_ANDAMENTO curto-circuita p/ vazio.
        var cenario = await SemearAgendamentoAsync(servicos: 1);
        var url = new Uri(MontarUrl("simples", cenario).OriginalString + "&status=EM_ANDAMENTO", UriKind.Relative);

        var response = await client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("data").GetArrayLength().Should().Be(0);
        corpo.GetProperty("message").GetString()
            .Should().Be("Nenhum evento encontrado para o período selecionado.");
    }

    // ---------------------------------------------------------------------
    // Extra — filtros opcionais clienteId/usuarioId/status realmente filtram.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task GET_filtro_clienteId_restringe_o_resultado()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var filialId = await SemearFilialVaziaAsync();
        var (clienteA, veiculoA) = await SemearClienteVeiculoAsync();
        var (clienteB, veiculoB) = await SemearClienteVeiculoAsync();

        var baseInicio = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc);
        var idA = await SemearAgendamentoCruAsync(filialId, clienteA, veiculoA, baseInicio);
        await SemearAgendamentoCruAsync(filialId, clienteB, veiculoB, baseInicio.AddHours(2));

        var url = new Uri(
            MontarUrl("simples", filialId, baseInicio.AddHours(-1), baseInicio.AddHours(4)).OriginalString
            + $"&clienteId={clienteA}",
            UriKind.Relative);
        var response = await client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = (await response.Content.ReadFromJsonAsync<JsonElement>(_json)).GetProperty("data");
        data.GetArrayLength().Should().Be(1);
        data[0].GetProperty("agendamentoId").GetGuid().Should().Be(idA);
    }

    [Fact]
    public async Task GET_filtro_usuarioId_filtra_pelo_responsavel()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var filialId = await SemearFilialVaziaAsync();
        var (clienteA, veiculoA) = await SemearClienteVeiculoAsync();
        var (clienteB, veiculoB) = await SemearClienteVeiculoAsync();
        var responsavelId = await SemearFiliadoAsync(clienteA);

        var baseInicio = new DateTime(2026, 8, 5, 10, 0, 0, DateTimeKind.Utc);

        // Apenas o primeiro agendamento tem o filiado como responsavel.
        var idComResponsavel = await SemearAgendamentoCruAsync(
            filialId, clienteA, veiculoA, baseInicio, responsavelId: responsavelId);
        await SemearAgendamentoCruAsync(
            filialId, clienteB, veiculoB, baseInicio.AddHours(2), responsavelId: null);

        var url = new Uri(
            MontarUrl("simples", filialId, baseInicio.AddHours(-1), baseInicio.AddHours(4)).OriginalString
            + $"&usuarioId={responsavelId}",
            UriKind.Relative);
        var response = await client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = (await response.Content.ReadFromJsonAsync<JsonElement>(_json)).GetProperty("data");
        data.GetArrayLength().Should().Be(1);
        data[0].GetProperty("agendamentoId").GetGuid().Should().Be(idComResponsavel);
    }

    [Fact]
    public async Task GET_filtro_status_cancelado_restringe_o_resultado()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var filialId = await SemearFilialVaziaAsync();
        var (clienteA, veiculoA) = await SemearClienteVeiculoAsync();
        var (clienteB, veiculoB) = await SemearClienteVeiculoAsync();

        var baseInicio = new DateTime(2026, 8, 10, 10, 0, 0, DateTimeKind.Utc);
        await SemearAgendamentoCruAsync(filialId, clienteA, veiculoA, baseInicio);
        var idCancelado = await SemearAgendamentoCruAsync(
            filialId, clienteB, veiculoB, baseInicio.AddHours(2), cancelar: true);

        var url = new Uri(
            MontarUrl("simples", filialId, baseInicio.AddHours(-1), baseInicio.AddHours(4)).OriginalString
            + "&status=CANCELADO",
            UriKind.Relative);
        var response = await client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = (await response.Content.ReadFromJsonAsync<JsonElement>(_json)).GetProperty("data");
        data.GetArrayLength().Should().Be(1);
        data[0].GetProperty("agendamentoId").GetGuid().Should().Be(idCancelado);
        data[0].GetProperty("status").GetString().Should().Be("CANCELADO");
    }

    // ---------------------------------------------------------------------
    // Extra — filial inexistente NAO da 404 (L6): cai em 200 com data: [].
    // ---------------------------------------------------------------------
    [Fact]
    public async Task GET_filial_inexistente_retorna_200_com_lista_vazia()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var inicio = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        var url = MontarUrl("simples", Guid.NewGuid(), inicio: inicio, fim: inicio.AddDays(1));
        var response = await client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("data").GetArrayLength().Should().Be(0);
    }

    // ---------------------------------------------------------------------
    // Extra — Cache-Control: no-store sempre presente (ADR 0004 L4 — PII).
    // ---------------------------------------------------------------------
    [Fact]
    public async Task GET_resposta_traz_cache_control_no_store()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var cenario = await SemearAgendamentoAsync(servicos: 1);

        var response = await client.GetAsync(MontarUrl("detalhado", cenario));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
    }

    // ---------------------------------------------------------------------
    // Extra — titulo/servicosResumo de fallback quando nao ha servicos.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task GET_simples_sem_servicos_usa_titulo_e_resumo_de_fallback()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var cenario = await SemearAgendamentoAsync(servicos: 0);

        var response = await client.GetAsync(MontarUrl("simples", cenario));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var item = (await response.Content.ReadFromJsonAsync<JsonElement>(_json))
            .GetProperty("data")[0];
        item.GetProperty("titulo").GetString().Should().Be("Agendamento");
        item.GetProperty("servicosResumo").GetString().Should().Be("Sem serviços");
    }

    // =====================================================================
    // Helpers
    // =====================================================================
    private static string Iso(DateTime utc) =>
        Uri.EscapeDataString(utc.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));

    private static Uri MontarUrl(string formato, Cenario cenario) =>
        MontarUrl(formato, cenario.FilialId, cenario.JanelaInicio, cenario.JanelaFim);

    private static Uri MontarUrl(string formato, Guid filialId, DateTime inicio, DateTime fim) =>
        new(
            $"/api/v1/agenda?formato={formato}&inicio={Iso(inicio)}&fim={Iso(fim)}&filialId={filialId}",
            UriKind.Relative);

    private async Task GarantirEnvelopeDeErroAsync(HttpResponseMessage response)
    {
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.TryGetProperty("errors", out _).Should().BeTrue("o 400 deve trazer o mapa de erros por campo");
    }

    private CarWashDbContext NovoDbContext() => CarWashDbContextFactoryForTests.Create(_fixture);

    private async Task<Guid> SemearFilialVaziaAsync()
    {
        await using var db = NovoDbContext();
        var filial = Filial.Criar(Guid.NewGuid(), $"Filial {Guid.NewGuid():N}"[..30], 4);
        db.Filiais.Add(filial);
        await db.SaveChangesAsync();
        return filial.Id;
    }

    private async Task<(Guid ClienteId, Guid VeiculoId)> SemearClienteVeiculoAsync()
    {
        await using var db = NovoDbContext();
        var cliente = ClienteValido();
        var veiculo = Veiculo.Criar(
            id: Guid.NewGuid(),
            clienteId: cliente.Id,
            placa: new Placa(GerarPlacaAleatoria()),
            modelo: "Civic",
            fabricante: "Honda",
            cor: "Preto");

        db.Clientes.Add(cliente);
        db.Veiculos.Add(veiculo);
        await db.SaveChangesAsync();
        return (cliente.Id, veiculo.Id);
    }

    /// <summary>
    /// Semeia um filiado (responsável) vinculado a um cliente e devolve o seu id —
    /// necessário porque <c>Agendamento.ResponsavelId</c> tem FK para <c>filiados</c>.
    /// </summary>
    private async Task<Guid> SemearFiliadoAsync(Guid clienteId)
    {
        await using var db = NovoDbContext();
        var filiado = Filiado.Criar(
            id: Guid.NewGuid(),
            clienteId: clienteId,
            nome: "Responsavel Teste",
            telefone: new Telefone("11987654321"),
            rg: "123456789");
        db.Filiados.Add(filiado);
        await db.SaveChangesAsync();
        return filiado.Id;
    }

    /// <summary>
    /// Semeia um agendamento "cru" (sem itens de serviço) numa filial dada.
    /// SaveChanges isolado garante CriadoEm distinto entre chamadas consecutivas.
    /// </summary>
    private async Task<Guid> SemearAgendamentoCruAsync(
        Guid filialId,
        Guid clienteId,
        Guid veiculoId,
        DateTime inicio,
        Guid? responsavelId = null,
        bool cancelar = false)
    {
        await using var db = NovoDbContext();
        var agendamento = Agendamento.Criar(
            id: Guid.NewGuid(),
            filialId: filialId,
            clienteId: clienteId,
            veiculoId: veiculoId,
            criadoPor: AdminId,
            inicio: inicio,
            fim: inicio.AddMinutes(30),
            responsavelId: responsavelId,
            observacoes: null,
            duracaoTotalMin: 0,
            valorTotal: 0m);

        if (cancelar)
        {
            agendamento.Cancelar();
        }

        db.Agendamentos.Add(agendamento);
        await db.SaveChangesAsync();
        return agendamento.Id;
    }

    /// <summary>
    /// Semeia uma filial vazia e dela deriva um cenário completo de 1 agendamento
    /// com a quantidade de serviços pedida, retornando todos os valores esperados.
    /// </summary>
    private async Task<Cenario> SemearAgendamentoAsync(int servicos)
    {
        await using var db = NovoDbContext();

        var filial = Filial.Criar(Guid.NewGuid(), $"Filial {Guid.NewGuid():N}"[..30], 4);
        var cliente = ClienteValido();
        var veiculo = Veiculo.Criar(
            id: Guid.NewGuid(),
            clienteId: cliente.Id,
            placa: new Placa(GerarPlacaAleatoria()),
            modelo: "Civic",
            fabricante: "Honda",
            cor: "Preto");

        var inicio = new DateTime(2026, 6, 15, 14, 0, 0, DateTimeKind.Utc);

        var nomes = new List<string>();
        var duracoes = new List<int>();
        var precos = new List<decimal>();
        var itensServico = new List<(Servico Servico, AgendamentoItem Item)>();

        var agendamentoId = Guid.NewGuid();
        int duracaoTotal = 0;
        decimal valorTotal = 0m;

        for (int i = 0; i < servicos; i++)
        {
            string nome = $"Servico {Guid.NewGuid():N}"[..20];
            int duracao = 30 + (i * 10);
            decimal precoCatalogo = 50m + (i * 5m);

            // Snapshot RN006: o preço/duração aplicados diferem do catálogo de propósito.
            int duracaoAplicada = duracao + 1;
            decimal precoAplicado = precoCatalogo + 7m;

            var servico = Servico.Criar(Guid.NewGuid(), nome, precoCatalogo, duracao);
            var item = AgendamentoItem.Criar(
                Guid.NewGuid(), agendamentoId, servico.Id, precoAplicado, duracaoAplicada);

            nomes.Add(nome);
            duracoes.Add(duracaoAplicada);
            precos.Add(precoAplicado);
            duracaoTotal += duracaoAplicada;
            valorTotal += precoAplicado;
            itensServico.Add((servico, item));
        }

        const string observacoes = "Cliente prefere o turno da tarde.";
        var agendamento = Agendamento.Criar(
            id: agendamentoId,
            filialId: filial.Id,
            clienteId: cliente.Id,
            veiculoId: veiculo.Id,
            criadoPor: AdminId,
            inicio: inicio,
            fim: inicio.AddMinutes(Math.Max(duracaoTotal, 30)),
            responsavelId: null,
            observacoes: observacoes,
            duracaoTotalMin: duracaoTotal,
            valorTotal: valorTotal);

        db.Filiais.Add(filial);
        db.Clientes.Add(cliente);
        db.Veiculos.Add(veiculo);
        db.Agendamentos.Add(agendamento);
        foreach (var (servico, item) in itensServico)
        {
            db.Servicos.Add(servico);
            db.AgendamentoItens.Add(item);
        }

        await db.SaveChangesAsync();

        return new Cenario
        {
            AgendamentoId = agendamentoId,
            FilialId = filial.Id,
            ClienteId = cliente.Id,
            ClienteNome = cliente.Nome,
            ClienteCpf = cliente.Cpf!,
            ClienteCelular = cliente.Celular,
            VeiculoId = veiculo.Id,
            VeiculoPlaca = veiculo.Placa,
            DuracaoTotalMin = duracaoTotal,
            ValorTotal = valorTotal,
            Observacoes = observacoes,
            ServicoNomes = nomes,
            ServicoDuracoes = duracoes,
            ServicoPrecos = precos,
            JanelaInicio = inicio.AddDays(-1),
            JanelaFim = inicio.AddDays(1),
        };
    }

    private static Cliente ClienteValido() => Cliente.Criar(
        id: Guid.NewGuid(),
        nome: "Cliente Teste",
        dataNascimento: new DateOnly(1990, 1, 1),
        celular: new Telefone("11987654321"),
        endereco: new Endereco(
            cep: "01310100",
            logradouro: "Av. Paulista",
            numero: "1000",
            complemento: null,
            bairro: "Bela Vista",
            cidade: "São Paulo",
            uf: "SP"),
        cpf: new Cpf(GerarCpfValido()));

    private static string GerarPlacaAleatoria()
    {
        var rng = Random.Shared;
        const string letras = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return $"{letras[rng.Next(26)]}{letras[rng.Next(26)]}{letras[rng.Next(26)]}"
            + $"{rng.Next(0, 10)}{letras[rng.Next(26)]}{rng.Next(0, 10)}{rng.Next(0, 10)}";
    }

    private static string GerarCpfValido()
    {
        Span<int> d = stackalloc int[11];
        var rng = Random.Shared;
        for (int i = 0; i < 9; i++)
        {
            d[i] = rng.Next(0, 10);
        }

        d[9] = Dv(d[..9], 10);
        d[10] = Dv(d[..10], 11);
        char[] chars = new char[11];
        for (int i = 0; i < 11; i++)
        {
            chars[i] = (char)('0' + d[i]);
        }

        return new string(chars);

        static int Dv(ReadOnlySpan<int> parcial, int pesoInicial)
        {
            int soma = 0;
            for (int i = 0; i < parcial.Length; i++)
            {
                soma += parcial[i] * (pesoInicial - i);
            }

            int resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    /// <summary>Valores semeados de um agendamento, usados para asserções do teste.</summary>
    private sealed record Cenario
    {
        public Guid AgendamentoId { get; init; }

        public Guid FilialId { get; init; }

        public Guid ClienteId { get; init; }

        public string ClienteNome { get; init; } = string.Empty;

        public string ClienteCpf { get; init; } = string.Empty;

        public string ClienteCelular { get; init; } = string.Empty;

        public Guid VeiculoId { get; init; }

        public string VeiculoPlaca { get; init; } = string.Empty;

        public int DuracaoTotalMin { get; init; }

        public decimal ValorTotal { get; init; }

        public string Observacoes { get; init; } = string.Empty;

        public IReadOnlyList<string> ServicoNomes { get; init; } = [];

        public IReadOnlyList<int> ServicoDuracoes { get; init; } = [];

        public IReadOnlyList<decimal> ServicoPrecos { get; init; } = [];

        public DateTime JanelaInicio { get; init; }

        public DateTime JanelaFim { get; init; }
    }
}
