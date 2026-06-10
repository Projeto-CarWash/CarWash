using System.Net;
using System.Net.Http.Json;
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
/// Cobertura de integração do <c>POST /api/v1/filiais</c> (RF017 + RF018).
/// Cumpre CA-204.1 (sucesso), CA-204.2 (campos/formato), CA-204.3
/// (duplicidade + race), CA-204.4 (e2e com pré-confirmação), CA-204.7 (401),
/// CA-204.10 (auditoria) — ADR-0007 §8.1.
/// </summary>
[Collection(nameof(PostgresCollection))]
public class CriarFilialEndpointTests : IAsyncDisposable
{
    private static readonly Uri RotaCriar = new("/api/v1/filiais", UriKind.Relative);
    private static readonly Uri RotaPreConfirmar =
        new("/api/v1/agendamentos/pre-confirmacao", UriKind.Relative);

    private readonly CarWashWebApplicationFactory _factory;
    private readonly PostgresFixture _fixture;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public CriarFilialEndpointTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    // CA-204.1
    [Fact]
    public async Task POST_valido_retorna_201_com_Location_e_envelope_id_mensagem_traceId()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var response = await client.PostAsJsonAsync(RotaCriar, PayloadValido(), _json);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.OriginalString.Should().StartWith("/api/v1/filiais/");

        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("id").GetGuid().Should().NotBeEmpty();
        corpo.GetProperty("mensagem").GetString().Should().Be("Filial cadastrada com sucesso.");
        corpo.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    // CA-204.1 — payload mínimo (sem cnpj, sem endereco) também aceito.
    [Fact]
    public async Task POST_payload_minimo_sem_cnpj_e_sem_endereco_retorna_201()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var response = await client.PostAsJsonAsync(RotaCriar, new
        {
            nome = $"FilialMin{Guid.NewGuid():N}"[..30],
            codigo = NovoCodigo(),
            celulasAtivas = 10,
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // CA-204.2 — nome inválido (vazio).
    [Fact]
    public async Task POST_nome_vazio_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var payload = PayloadValido();
        payload["nome"] = string.Empty;

        var response = await client.PostAsJsonAsync(RotaCriar, payload, _json);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("errors").TryGetProperty("nome", out _).Should().BeTrue();
    }

    // CA-204.2 — UF inválida (comprimento errado: 3 caracteres).
    [Fact]
    public async Task POST_uf_invalida_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var payload = PayloadValido();
        payload["endereco"] = new
        {
            cep = "01310100",
            logradouro = "Av. Paulista",
            numero = "1000",
            bairro = "Bela Vista",
            cidade = "São Paulo",
            uf = "ZZZ", // 3 caracteres: pega Length(2) do validator
        };

        var response = await client.PostAsJsonAsync(RotaCriar, payload, _json);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // C5 — UF com 2 caracteres mas fora da lista das 27 UFs deve cair no
    // validator (400) e NÃO escapar para o VO `Endereco` (que viraria 500).
    [Fact]
    public async Task POST_uf_dois_caracteres_fora_da_lista_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var payload = PayloadValido();
        payload["endereco"] = new
        {
            cep = "01310100",
            logradouro = "Av. Paulista",
            numero = "1000",
            bairro = "Bela Vista",
            cidade = "São Paulo",
            uf = "XX", // UF inexistente, mas com 2 caracteres
        };

        var response = await client.PostAsJsonAsync(RotaCriar, payload, _json);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);

        // ValidationProblems.NormalizarCampo apenas baixa o 1º caractere do
        // PropertyName do FluentValidation: "Endereco.Uf" → "endereco.Uf".
        corpo.GetProperty("errors").TryGetProperty("endereco.Uf", out _).Should().BeTrue();
    }

    // CA-204.2 — células fora da faixa.
    [Fact]
    public async Task POST_celulas_acima_de_100_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var payload = PayloadValido();
        payload["celulasAtivas"] = 999;

        var response = await client.PostAsJsonAsync(RotaCriar, payload, _json);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("errors").TryGetProperty("celulasAtivas", out _).Should().BeTrue();
    }

    // CA-204.3 — código duplicado retorna 409 com slug.
    [Fact]
    public async Task POST_codigo_duplicado_retorna_409_filial_codigo_ja_existe()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        string codigo = NovoCodigo();
        var primeiro = PayloadValido();
        primeiro["codigo"] = codigo;

        var r1 = await client.PostAsJsonAsync(RotaCriar, primeiro, _json);
        r1.StatusCode.Should().Be(HttpStatusCode.Created);

        var segundo = PayloadValido();
        segundo["codigo"] = codigo;
        var r2 = await client.PostAsJsonAsync(RotaCriar, segundo, _json);

        r2.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var corpo = await r2.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("type").GetString().Should().Contain("filial-codigo-ja-existe");
    }

    // CA-204.3 — CNPJ duplicado retorna 409 com slug.
    [Fact]
    public async Task POST_cnpj_duplicado_retorna_409_filial_cnpj_ja_existe()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        string cnpj = NovoCnpjValido();
        var primeiro = PayloadValido();
        primeiro["cnpj"] = cnpj;

        var r1 = await client.PostAsJsonAsync(RotaCriar, primeiro, _json);
        r1.StatusCode.Should().Be(HttpStatusCode.Created);

        var segundo = PayloadValido();
        segundo["cnpj"] = cnpj;
        var r2 = await client.PostAsJsonAsync(RotaCriar, segundo, _json);

        r2.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var corpo = await r2.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("type").GetString().Should().Contain("filial-cnpj-ja-existe");
    }

    // CA-204.3 — nome duplicado case-insensitive retorna 409 com slug.
    [Fact]
    public async Task POST_nome_duplicado_case_insensitive_retorna_409_filial_nome_ja_existe()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        string nome = $"FilNome{Guid.NewGuid():N}"[..20];
        var primeiro = PayloadValido();
        primeiro["nome"] = nome;

        var r1 = await client.PostAsJsonAsync(RotaCriar, primeiro, _json);
        r1.StatusCode.Should().Be(HttpStatusCode.Created);

        var segundo = PayloadValido();
        segundo["nome"] = nome.ToUpperInvariant(); // mesmo nome em outra caixa
        var r2 = await client.PostAsJsonAsync(RotaCriar, segundo, _json);

        r2.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var corpo = await r2.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("type").GetString().Should().Contain("filial-nome-ja-existe");
    }

    // C1 — nome com curingas LIKE literais (`%` e `_`) NÃO pode disparar 409
    // falso. A consulta deve usar igualdade case-insensitive (LOWER), e não
    // ILIKE — caso contrário "FOO%" casaria com "FOOBAR" via wildcard.
    [Fact]
    public async Task POST_nome_com_curingas_like_literais_nao_gera_409_falso()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        string sufixo = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        string nomeBase = $"FOO{sufixo}BAR";
        string nomeComPercent = $"FOO{sufixo}%";
        string nomeComUnderscore = $"FOO{sufixo}_";

        // Cria primeiro o nome "concreto" que serviria de gabarito caso a
        // implementação errada usasse ILIKE com wildcards.
        var primeiro = PayloadValido();
        primeiro["nome"] = nomeBase;
        var r1 = await client.PostAsJsonAsync(RotaCriar, primeiro, _json);
        r1.StatusCode.Should().Be(HttpStatusCode.Created);

        // Agora tenta cadastrar dois nomes contendo `%` e `_` literais. Com
        // LOWER+igualdade, nenhum deles colide com `nomeBase`.
        var segundo = PayloadValido();
        segundo["nome"] = nomeComPercent;
        var r2 = await client.PostAsJsonAsync(RotaCriar, segundo, _json);
        r2.StatusCode.Should().Be(HttpStatusCode.Created,
            "nome com `%` literal não pode casar com outro nome via wildcard");

        var terceiro = PayloadValido();
        terceiro["nome"] = nomeComUnderscore;
        var r3 = await client.PostAsJsonAsync(RotaCriar, terceiro, _json);
        r3.StatusCode.Should().Be(HttpStatusCode.Created,
            "nome com `_` literal não pode casar com outro nome via wildcard");
    }

    // CA-204.3 — race condition: dois POSTs concorrentes com mesmo código.
    // Exatamente uma vence; a outra recebe 409 traduzido pelo repositório a
    // partir de DbUpdateException → uk_filiais_codigo.
    [Fact]
    public async Task POST_concorrente_mesmo_codigo_apenas_um_vence_outro_409()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        string codigo = NovoCodigo();

        var p1 = PayloadValido();
        p1["codigo"] = codigo;
        var p2 = PayloadValido();
        p2["codigo"] = codigo;

        var t1 = client.PostAsJsonAsync(RotaCriar, p1, _json);
        var t2 = client.PostAsJsonAsync(RotaCriar, p2, _json);
        var resultados = await Task.WhenAll(t1, t2);

        int sucesso = resultados.Count(r => r.StatusCode == HttpStatusCode.Created);
        int conflito = resultados.Count(r => r.StatusCode == HttpStatusCode.Conflict);
        sucesso.Should().Be(1, "apenas uma das tentativas concorrentes deve vencer");
        conflito.Should().Be(1, "a perdedora deve receber 409 traduzido do UK violation");
    }

    // CA-204.4 — após cadastrar a filial, o agendamento.pre-confirmacao a
    // reconhece (não retorna 404 de filial). Pode retornar 200 ou bloquear
    // por outra invariante, mas nunca falhar com filial-nao-encontrada.
    [Fact]
    public async Task POST_filial_aparece_em_pre_confirmacao_de_agendamento()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        // Cria a filial via endpoint público.
        var criacao = await client.PostAsJsonAsync(RotaCriar, PayloadValido(), _json);
        criacao.StatusCode.Should().Be(HttpStatusCode.Created);
        var corpoCriacao = await criacao.Content.ReadFromJsonAsync<JsonElement>(_json);
        var filialId = corpoCriacao.GetProperty("id").GetGuid();

        // Cria cliente + veículo direto no banco (slices ortogonais).
        var (clienteId, veiculoId, servicoIds) = await SemearClienteVeiculoServicosAsync();

        var preconf = await client.PostAsJsonAsync(RotaPreConfirmar, new
        {
            filialId,
            clienteId,
            veiculoId,
            inicio = DateTime.UtcNow.AddDays(1),
            servicoIds,
        }, _json);

        // O ponto-chave: a filial recém-criada NÃO retorna 404 de
        // "filial não encontrada". Aceitamos 200 (sucesso) ou 422 (outra
        // invariante) — qualquer 404 falha o teste.
        preconf.StatusCode.Should().NotBe(HttpStatusCode.NotFound,
            "a filial recém-criada deve ser visível para o agendamento (CA-204.4)");

        if (preconf.StatusCode != HttpStatusCode.OK)
        {
            var detalhe = await preconf.Content.ReadFromJsonAsync<JsonElement>(_json);
            detalhe.GetProperty("type").GetString().Should().NotContain("filial",
                "qualquer 4xx aqui não pode ser sobre a filial existir");
        }
    }

    // CA-204.7
    [Fact]
    public async Task POST_sem_token_retorna_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(RotaCriar, PayloadValido(), _json);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // CA-204.10 — audit_log gerado com evento FilialCriada + correlationId + usuarioId.
    [Fact]
    public async Task POST_valido_gera_audit_log_com_evento_correlationId_e_usuarioId()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var response = await client.PostAsJsonAsync(RotaCriar, PayloadValido(), _json);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        var filialId = corpo.GetProperty("id").GetGuid();

        await using var db = NovoDbContext();
        var log = await db.AuditLogs
            .AsNoTracking()
            .Where(x => x.EntidadeId == filialId)
            .OrderByDescending(x => x.CriadoEm)
            .FirstOrDefaultAsync();

        log.Should().NotBeNull("audit_log deve ter sido emitido pelo AuditLogInterceptor");
        log!.Evento.Should().Be("FilialCriada");
        log.CorrelationId.Should().NotBeNullOrWhiteSpace();
        log.UsuarioId.Should().NotBeNull("o admin autenticado deve aparecer no log");
    }

    private async Task<(Guid ClienteId, Guid VeiculoId, IReadOnlyList<Guid> ServicoIds)>
        SemearClienteVeiculoServicosAsync()
    {
        await using var db = NovoDbContext();

        var cliente = ClienteValido();
        var veiculo = CarWash.Domain.Entities.Veiculo.Criar(
            id: Guid.NewGuid(),
            clienteId: cliente.Id,
            placa: new CarWash.Domain.ValueObjects.Placa(GerarPlacaAleatoria()),
            modelo: "Civic",
            fabricante: "Honda",
            cor: "Preto");

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

        return (cliente.Id, veiculo.Id, servicoIds);
    }

    private CarWashDbContext NovoDbContext() => CarWashDbContextFactoryForTests.Create(_fixture);

    private static Dictionary<string, object?> PayloadValido() => new()
    {
        ["nome"] = $"Filial{Guid.NewGuid():N}"[..20],
        ["codigo"] = NovoCodigo(),
        ["celulasAtivas"] = 30,
        ["endereco"] = new
        {
            cep = "01310100",
            logradouro = "Av. Paulista",
            numero = "1000",
            bairro = "Bela Vista",
            cidade = "São Paulo",
            uf = "SP",
        },
    };

    private static string NovoCodigo() => $"F{Guid.NewGuid():N}"[..10].ToUpperInvariant();

    private static CarWash.Domain.Entities.Cliente ClienteValido() => CarWash.Domain.Entities.Cliente.Criar(
        id: Guid.NewGuid(),
        nome: "Cliente Teste",
        dataNascimento: new DateOnly(1990, 1, 1),
        celular: new CarWash.Domain.ValueObjects.Telefone("11987654321"),
        endereco: new CarWash.Domain.ValueObjects.Endereco(
            cep: "01310100",
            logradouro: "Av. Paulista",
            numero: "1000",
            complemento: null,
            bairro: "Bela Vista",
            cidade: "São Paulo",
            uf: "SP"),
        cpf: new CarWash.Domain.ValueObjects.Cpf(GerarCpfValido()));

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

    private static string NovoCnpjValido()
    {
        Span<int> d = stackalloc int[14];
        var rng = Random.Shared;
        for (int i = 0; i < 12; i++)
        {
            d[i] = rng.Next(0, 10);
        }

        // Evita sequência repetida (rejeitada pelo VO Cnpj).
        bool todosIguais = true;
        for (int i = 1; i < 12; i++)
        {
            if (d[i] != d[0])
            {
                todosIguais = false;
                break;
            }
        }

        if (todosIguais)
        {
            d[0] = (d[0] + 1) % 10;
        }

        d[12] = DvCnpj(d[..12], [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
        d[13] = DvCnpj(d[..13], [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);

        char[] chars = new char[14];
        for (int i = 0; i < 14; i++)
        {
            chars[i] = (char)('0' + d[i]);
        }

        return new string(chars);

        static int DvCnpj(ReadOnlySpan<int> parcial, int[] pesos)
        {
            int soma = 0;
            for (int i = 0; i < parcial.Length; i++)
            {
                soma += parcial[i] * pesos[i];
            }

            int resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
