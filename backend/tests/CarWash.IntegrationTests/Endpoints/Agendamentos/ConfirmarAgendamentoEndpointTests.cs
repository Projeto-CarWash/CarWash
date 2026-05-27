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

namespace CarWash.IntegrationTests.Endpoints.Agendamentos;

/// <summary>
/// Cobre o RF015 — etapa 2 (<c>POST /api/v1/agendamentos/confirmar</c>) ponta a
/// ponta com Testcontainers + PostgreSQL real. Valida o checklist QA do card 133
/// (itens 1–11) mais a divergência de resumo e a race condition de idempotência.
/// </summary>
[Collection(nameof(PostgresCollection))]
public class ConfirmarAgendamentoEndpointTests : IAsyncDisposable
{
    private static readonly Uri RotaPreConfirmar =
        new("/api/v1/agendamentos/pre-confirmacao", UriKind.Relative);

    private static readonly Uri RotaConfirmar =
        new("/api/v1/agendamentos/confirmar", UriKind.Relative);

    private readonly CarWashWebApplicationFactory _factory;
    private readonly PostgresFixture _fixture;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public ConfirmarAgendamentoEndpointTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    // ---------------------------------------------------------------------
    // Item 2 — confirmação válida cria agendamento + itens com 201.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task POST_confirmacao_valida_retorna_201_e_persiste_agendamento_com_itens()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var ctx = await PrepararConfirmacaoAsync(client);

        var response = await client.PostAsJsonAsync(RotaConfirmar, ctx.PayloadConfirmacao(), _json);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.OriginalString.Should().StartWith("/api/v1/agendamentos/");

        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        var agendamentoId = corpo.GetProperty("id").GetGuid();
        agendamentoId.Should().NotBeEmpty();
        corpo.GetProperty("status").GetString().Should().Be("agendado");
        corpo.GetProperty("itens").GetArrayLength().Should().Be(ctx.ServicoIds.Count);

        // O registro de idempotência foi gravado na mesma transação.
        await using var db = NovoDbContext();
        (await db.Agendamentos.CountAsync(a => a.Id == agendamentoId)).Should().Be(1);
        (await db.AgendamentoItens.CountAsync(i => i.AgendamentoId == agendamentoId))
            .Should().Be(ctx.ServicoIds.Count);
        (await db.AgendamentoHistoricos.CountAsync(h => h.AgendamentoId == agendamentoId))
            .Should().BeGreaterThanOrEqualTo(1);
        (await db.IdempotenciaRequisicoes.CountAsync(x => x.IdempotencyKey == ctx.IdempotencyKey))
            .Should().Be(1);
    }

    // ---------------------------------------------------------------------
    // Item 3 — confirmar ausente / false → 400 com mensagem do contrato.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task POST_sem_campo_confirmar_retorna_400_com_mensagem_do_contrato()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var ctx = await PrepararConfirmacaoAsync(client);

        var payload = ctx.PayloadConfirmacao();
        payload.Remove("confirmar");

        var response = await client.PostAsJsonAsync(RotaConfirmar, payload, _json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ExtrairErrosAsync(response)).Should()
            .Contain(m => m.Contains("Confirmação explícita é obrigatória", StringComparison.Ordinal));
    }

    [Fact]
    public async Task POST_com_confirmar_false_retorna_400_com_mensagem_do_contrato()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var ctx = await PrepararConfirmacaoAsync(client);

        var payload = ctx.PayloadConfirmacao();
        payload["confirmar"] = false;

        var response = await client.PostAsJsonAsync(RotaConfirmar, payload, _json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ExtrairErrosAsync(response)).Should()
            .Contain(m => m.Contains("Confirmação explícita é obrigatória", StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------------
    // Item 4 — tokenConfirmacao inválido → 400 com mensagem do contrato.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task POST_token_invalido_retorna_400_com_mensagem_do_contrato()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var ctx = await PrepararConfirmacaoAsync(client);

        // Token sintaticamente plausível, mas com assinatura inválida.
        var payload = ctx.PayloadConfirmacao();
        payload["tokenConfirmacao"] = "cGF5bG9hZC1mYWxzbw.YXNzaW5hdHVyYS1mYWxzYQ";

        var response = await client.PostAsJsonAsync(RotaConfirmar, payload, _json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ExtrairErrosAsync(response)).Should()
            .Contain(m => m.Contains("Token de confirmação inválido", StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------------
    // Item 5 — tokenConfirmacao expirado → 410 com mensagem do contrato.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task POST_token_expirado_retorna_410_com_mensagem_do_contrato()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var ctx = await PrepararConfirmacaoAsync(client);

        // Token íntegro em assinatura, porém com exp no passado (sintético).
        var payload = ctx.PayloadConfirmacao();
        payload["tokenConfirmacao"] = TokenConfirmacaoTestHelper.GerarExpiradoApartirDe(ctx.Token);

        var response = await client.PostAsJsonAsync(RotaConfirmar, payload, _json);

        response.StatusCode.Should().Be(HttpStatusCode.Gone);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("type").GetString().Should().Contain("sessao-confirmacao-expirada");
        corpo.GetProperty("title").GetString().Should()
            .Be("Sessão de confirmação expirada. Gere uma nova pré-confirmação.");

        // Nada persistido após o 410.
        await using var db = NovoDbContext();
        (await db.Agendamentos.CountAsync(a => a.VeiculoId == ctx.VeiculoId)).Should().Be(0);
    }

    // ---------------------------------------------------------------------
    // Item 6 — mesma idempotencyKey + mesmo payload → não duplica, 201.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task POST_mesma_chave_mesmo_payload_nao_duplica_e_retorna_201()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var ctx = await PrepararConfirmacaoAsync(client);
        var payload = ctx.PayloadConfirmacao();

        var primeira = await client.PostAsJsonAsync(RotaConfirmar, payload, _json);
        primeira.StatusCode.Should().Be(HttpStatusCode.Created);
        var idPrimeira = (await primeira.Content.ReadFromJsonAsync<JsonElement>(_json))
            .GetProperty("id").GetGuid();

        var segunda = await client.PostAsJsonAsync(RotaConfirmar, payload, _json);
        segunda.StatusCode.Should().Be(HttpStatusCode.Created);
        var idSegunda = (await segunda.Content.ReadFromJsonAsync<JsonElement>(_json))
            .GetProperty("id").GetGuid();

        // Mesmo recurso devolvido, e o replay é sinalizado pelo header.
        idSegunda.Should().Be(idPrimeira);
        segunda.Headers.Contains("Idempotent-Replay").Should().BeTrue();

        await using var db = NovoDbContext();
        (await db.Agendamentos.CountAsync(a => a.VeiculoId == ctx.VeiculoId)).Should().Be(1);
    }

    // ---------------------------------------------------------------------
    // Item 7 — mesma idempotencyKey + payload diferente → 409.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task POST_mesma_chave_payload_diferente_retorna_409()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var ctx = await PrepararConfirmacaoAsync(client);

        var primeira = await client.PostAsJsonAsync(RotaConfirmar, ctx.PayloadConfirmacao(), _json);
        primeira.StatusCode.Should().Be(HttpStatusCode.Created);

        // Nova prévia (resumo diferente: outro horário) reaproveitando a MESMA chave.
        var outro = await PrepararConfirmacaoAsync(client, inicioOffsetDias: 8);
        var payloadConflitante = outro.PayloadConfirmacao();
        payloadConflitante["idempotencyKey"] = ctx.IdempotencyKey;

        var segunda = await client.PostAsJsonAsync(RotaConfirmar, payloadConflitante, _json);

        segunda.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var corpo = await segunda.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("type").GetString().Should().Contain("idempotencia-conflito");
    }

    // ---------------------------------------------------------------------
    // Item 8 — conflito de horário (RN011) entre prévia e confirmação → 409.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task POST_conflito_de_horario_entre_previa_e_confirmacao_retorna_409()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var ctx = await PrepararConfirmacaoAsync(client);

        // Entre a prévia e a confirmação, o horário do veículo é tomado por outro
        // agendamento direto (RF007). A confirmação deve então colidir.
        var ocupacao = await client.PostAsJsonAsync(
            new Uri("/api/v1/agendamentos", UriKind.Relative),
            new
            {
                filialId = ctx.FilialId,
                clienteId = ctx.ClienteId,
                veiculoId = ctx.VeiculoId,
                inicio = ctx.Inicio,
                servicoIds = ctx.ServicoIds,
            },
            _json);
        ocupacao.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await client.PostAsJsonAsync(RotaConfirmar, ctx.PayloadConfirmacao(), _json);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("type").GetString().Should().Contain("agendamento-conflito-veiculo");
        corpo.GetProperty("title").GetString().Should()
            .Be("O horário não está mais disponível. Atualize e confirme novamente.");
    }

    // ---------------------------------------------------------------------
    // Item 9 — sem autenticação → 401.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task POST_sem_token_de_acesso_retorna_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(RotaConfirmar, new
        {
            confirmar = true,
            tokenConfirmacao = "x.y",
            idempotencyKey = Guid.NewGuid(),
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------------------------------------------------------------------
    // Item 10 — sem permissão → 403.
    //
    // NÃO-TESTÁVEL no MVP: o grupo /api/v1/agendamentos usa apenas
    // .RequireAuthorization() — sem policy de role. Administrador e Funcionário
    // têm o mesmo acesso (decisão de produto do MVP). Não existe caminho que,
    // para um usuário AUTENTICADO, retorne 403 neste endpoint. O contrato 403 do
    // .ProducesProblem está apenas preparado para uma futura segregação de
    // permissões. Mantido como Skip rastreável (card 133, observação de QA) para
    // o item ficar visível no relatório de CI em vez de silenciosamente ausente.
    // ---------------------------------------------------------------------
    [Fact(Skip = "Card 133 / item 10: 403 não tem caminho no MVP — endpoint só "
        + "exige autenticação, sem policy de role. Contrato preparado para o futuro.")]
    public Task POST_sem_permissao_retorna_403()
    {
        // Sem regra de autorização por papel, não há como exercitar o 403 aqui.
        return Task.CompletedTask;
    }

    // ---------------------------------------------------------------------
    // Item 11 — erro interno não vaza detalhes técnicos.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task POST_caminho_feliz_nao_vaza_stack_trace_em_eventual_5xx()
    {
        // Guarda de regressão: o caminho feliz não retorna 5xx; se algum dia
        // retornar, o corpo não pode conter stack trace nem SQL.
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var ctx = await PrepararConfirmacaoAsync(client);

        var response = await client.PostAsJsonAsync(RotaConfirmar, ctx.PayloadConfirmacao(), _json);

        if ((int)response.StatusCode >= 500)
        {
            string corpo = await response.Content.ReadAsStringAsync();
            corpo.Should().NotContain("at CarWash.");
            corpo.Should().NotContain("Exception:");
            corpo.Should().NotContainEquivalentOf("npgsql");
            corpo.Should().NotContain("SELECT ");
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }
    }

    // ---------------------------------------------------------------------
    // Extra — divergência de resumo (hash do payload ≠ hash do token) → 409.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task POST_resumo_divergente_retorna_409_com_slug_proprio()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var ctx = await PrepararConfirmacaoAsync(client);

        // Token íntegro/assinado mas com hashResumo que não bate com o recálculo.
        var payload = ctx.PayloadConfirmacao();
        payload["tokenConfirmacao"] = TokenConfirmacaoTestHelper.GerarComHashDivergente(ctx.Token);

        var response = await client.PostAsJsonAsync(RotaConfirmar, payload, _json);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("type").GetString().Should().Contain("agendamento-resumo-divergente");
        corpo.GetProperty("title").GetString().Should()
            .Be("Os dados do agendamento foram alterados. Revise antes de confirmar.");

        await using var db = NovoDbContext();
        (await db.Agendamentos.CountAsync(a => a.VeiculoId == ctx.VeiculoId)).Should().Be(0);
    }

    /// <summary>
    /// Verifica que os três tipos de 409 do RF015 carregam <c>type</c> distintos
    /// no ProblemDetails — base para o frontend discriminar pelo <c>type</c> em
    /// vez de regex de texto.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task POST_os_tres_409_tem_type_distintos_no_problem_details()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        // 409 (a) — divergência de resumo.
        var ctxDivergente = await PrepararConfirmacaoAsync(client);
        var payloadDivergente = ctxDivergente.PayloadConfirmacao();
        payloadDivergente["tokenConfirmacao"] =
            TokenConfirmacaoTestHelper.GerarComHashDivergente(ctxDivergente.Token);
        var respDivergente = await client.PostAsJsonAsync(RotaConfirmar, payloadDivergente, _json);
        string? typeDivergente = (await respDivergente.Content.ReadFromJsonAsync<JsonElement>(_json))
            .GetProperty("type").GetString();

        // 409 (b) — conflito de idempotência.
        var ctxIdem = await PrepararConfirmacaoAsync(client);
        (await client.PostAsJsonAsync(RotaConfirmar, ctxIdem.PayloadConfirmacao(), _json))
            .StatusCode.Should().Be(HttpStatusCode.Created);
        var outro = await PrepararConfirmacaoAsync(client, inicioOffsetDias: 9);
        var payloadIdem = outro.PayloadConfirmacao();
        payloadIdem["idempotencyKey"] = ctxIdem.IdempotencyKey;
        var respIdem = await client.PostAsJsonAsync(RotaConfirmar, payloadIdem, _json);
        string? typeIdem = (await respIdem.Content.ReadFromJsonAsync<JsonElement>(_json))
            .GetProperty("type").GetString();

        // 409 (c) — conflito de veículo (RN011).
        var ctxConflito = await PrepararConfirmacaoAsync(client);
        (await client.PostAsJsonAsync(
            new Uri("/api/v1/agendamentos", UriKind.Relative),
            new
            {
                filialId = ctxConflito.FilialId,
                clienteId = ctxConflito.ClienteId,
                veiculoId = ctxConflito.VeiculoId,
                inicio = ctxConflito.Inicio,
                servicoIds = ctxConflito.ServicoIds,
            },
            _json)).StatusCode.Should().Be(HttpStatusCode.Created);
        var respConflito = await client.PostAsJsonAsync(RotaConfirmar, ctxConflito.PayloadConfirmacao(), _json);
        string? typeConflito = (await respConflito.Content.ReadFromJsonAsync<JsonElement>(_json))
            .GetProperty("type").GetString();

        typeDivergente.Should().EndWith("agendamento-resumo-divergente");
        typeIdem.Should().EndWith("idempotencia-conflito");
        typeConflito.Should().EndWith("agendamento-conflito-veiculo");
        new[] { typeDivergente, typeIdem, typeConflito }.Distinct().Should().HaveCount(3);
    }

    // ---------------------------------------------------------------------
    // Extra — race condition: duplo clique concorrente cria 1 só agendamento.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task POST_duplo_clique_concorrente_mesma_chave_cria_um_so_agendamento()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var ctx = await PrepararConfirmacaoAsync(client);

        // Dois clientes HTTP separados disparam a MESMA confirmação (mesmo token e
        // mesma idempotencyKey) em paralelo — simula o duplo clique do usuário.
        var clienteA = await AuthenticatedHttpClient.CreateAsync(_factory);
        var clienteB = await AuthenticatedHttpClient.CreateAsync(_factory);

        var t1 = clienteA.PostAsJsonAsync(RotaConfirmar, ctx.PayloadConfirmacao(), _json);
        var t2 = clienteB.PostAsJsonAsync(RotaConfirmar, ctx.PayloadConfirmacao(), _json);
        var respostas = await Task.WhenAll(t1, t2);

        // Invariante crítico do card 133: o duplo clique cria UM ÚNICO agendamento.
        // Na corrida, a perdedora pode resolver de duas formas, ambas corretas:
        //   • a UNIQUE uq_idempotencia_key_escopo dispara → replay → 201 (mesmo id);
        //   • a EXCLUDE ex_ag_veiculo_janela dispara antes → 409 conflito de veículo.
        // O que NUNCA pode acontecer é dois agendamentos persistidos.
        respostas.Should().AllSatisfy(r =>
            r.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.Conflict));
        respostas.Count(r => r.StatusCode == HttpStatusCode.Created)
            .Should().BeGreaterThanOrEqualTo(1, "ao menos uma confirmação deve concluir");

        // Quando ambas devolvem 201, devem apontar para o MESMO agendamento (replay).
        var criadas = respostas.Where(r => r.StatusCode == HttpStatusCode.Created).ToList();
        if (criadas.Count == 2)
        {
            var ids = new List<Guid>();
            foreach (var r in criadas)
            {
                ids.Add((await r.Content.ReadFromJsonAsync<JsonElement>(_json)).GetProperty("id").GetGuid());
            }

            ids.Distinct().Should().HaveCount(1, "o replay idempotente devolve o mesmo recurso");
        }

        await using var db = NovoDbContext();
        (await db.Agendamentos.CountAsync(a => a.VeiculoId == ctx.VeiculoId))
            .Should().Be(1, "duplo clique concorrente nunca pode duplicar o agendamento");
        (await db.IdempotenciaRequisicoes.CountAsync(x => x.IdempotencyKey == ctx.IdempotencyKey))
            .Should().BeLessThanOrEqualTo(1, "no máximo um registro de idempotência por chave");
    }

    // =====================================================================
    // Infraestrutura de teste
    // =====================================================================

    /// <summary>
    /// Estado de uma pré-confirmação real: dependências semeadas, token emitido
    /// pelo endpoint e a <c>idempotencyKey</c> escolhida para a confirmação.
    /// </summary>
    private sealed record ContextoConfirmacao(
        Guid FilialId,
        Guid ClienteId,
        Guid VeiculoId,
        IReadOnlyList<Guid> ServicoIds,
        DateTime Inicio,
        string Token,
        Guid IdempotencyKey)
    {
        /// <summary>Monta o payload de confirmação como dicionário mutável.</summary>
        public Dictionary<string, object?> PayloadConfirmacao() => new(StringComparer.Ordinal)
        {
            ["filialId"] = FilialId,
            ["clienteId"] = ClienteId,
            ["veiculoId"] = VeiculoId,
            ["inicio"] = Inicio,
            ["servicoIds"] = ServicoIds,
            ["confirmar"] = true,
            ["tokenConfirmacao"] = Token,
            ["idempotencyKey"] = IdempotencyKey,
        };
    }

    /// <summary>
    /// Semeia dependências, executa a pré-confirmação real e devolve o contexto
    /// pronto para a etapa de confirmação.
    /// </summary>
    private async Task<ContextoConfirmacao> PrepararConfirmacaoAsync(
        HttpClient client,
        int inicioOffsetDias = 1)
    {
        var (filialId, clienteId, veiculoId, servicoIds) = await SemearDependenciasAsync();
        var inicio = DateTime.UtcNow.AddDays(inicioOffsetDias).AddHours(Random.Shared.Next(0, 12));

        var pre = await client.PostAsJsonAsync(RotaPreConfirmar, new
        {
            filialId,
            clienteId,
            veiculoId,
            inicio,
            servicoIds,
        }, _json);
        pre.StatusCode.Should().Be(HttpStatusCode.OK, "a pré-confirmação é pré-requisito da confirmação");

        var corpo = await pre.Content.ReadFromJsonAsync<JsonElement>(_json);
        string token = corpo.GetProperty("tokenConfirmacao").GetString()!;

        return new ContextoConfirmacao(
            filialId,
            clienteId,
            veiculoId,
            servicoIds,
            inicio,
            token,
            Guid.NewGuid());
    }

    private async Task<List<string>> ExtrairErrosAsync(HttpResponseMessage response)
    {
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        var mensagens = new List<string>();

        if (corpo.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
        {
            mensagens.Add(title.GetString()!);
        }

        if (corpo.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
        {
            foreach (var campo in errors.EnumerateObject())
            {
                foreach (var msg in campo.Value.EnumerateArray())
                {
                    mensagens.Add(msg.GetString()!);
                }
            }
        }

        return mensagens;
    }

    private async Task<(Guid FilialId, Guid ClienteId, Guid VeiculoId, IReadOnlyList<Guid> ServicoIds)>
        SemearDependenciasAsync()
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

        db.Filiais.Add(filial);
        db.Clientes.Add(cliente);
        db.Veiculos.Add(veiculo);
        await db.SaveChangesAsync();

        var servicoIds = await db.Servicos
            .AsNoTracking()
            .Where(s => s.Ativo)
            .OrderBy(s => s.Nome)
            .Select(s => s.Id)
            .Take(2)
            .ToListAsync();

        return (filial.Id, cliente.Id, veiculo.Id, servicoIds);
    }

    private CarWashDbContext NovoDbContext() => CarWashDbContextFactoryForTests.Create(_fixture);

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
        return $"{letras[rng.Next(26)]}{letras[rng.Next(26)]}{letras[rng.Next(26)]}{rng.Next(0, 10)}{letras[rng.Next(26)]}{rng.Next(0, 10)}{rng.Next(0, 10)}";
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
}
