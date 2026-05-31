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
/// Cobre RF007/RF019/RF020 e os critérios de aceite CA006 (conflito de veículo
/// na mesma ou em filiais diferentes) e CA007 (filial obrigatória) ponta a ponta.
/// </summary>
[Collection(nameof(PostgresCollection))]
public class CriarAgendamentoEndpointTests : IAsyncDisposable
{
    private static readonly Uri RotaCriar = new("/api/v1/agendamentos", UriKind.Relative);

    private readonly CarWashWebApplicationFactory _factory;
    private readonly PostgresFixture _fixture;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public CriarAgendamentoEndpointTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    [Fact]
    public async Task POST_valido_retorna_201_com_totais()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var (filialId, clienteId, veiculoId, servicoIds) = await SemearDependenciasAsync();

        var response = await client.PostAsJsonAsync(RotaCriar, new
        {
            filialId,
            clienteId,
            veiculoId,
            inicio = DateTime.UtcNow.AddDays(1),
            servicoIds,
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.OriginalString.Should().StartWith("/api/v1/agendamentos/");

        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("id").GetGuid().Should().NotBeEmpty();
        corpo.GetProperty("status").GetString().Should().Be("agendado");
        corpo.GetProperty("duracaoTotalMin").GetInt32().Should().BeGreaterThan(0);
        corpo.GetProperty("valorTotal").GetDecimal().Should().BeGreaterThan(0m);
        corpo.GetProperty("itens").GetArrayLength().Should().Be(servicoIds.Count);
    }

    [Fact]
    public async Task POST_sem_token_retorna_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(RotaCriar, new { }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_sem_filial_retorna_400_CA007()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var (_, clienteId, veiculoId, servicoIds) = await SemearDependenciasAsync();

        var response = await client.PostAsJsonAsync(RotaCriar, new
        {
            filialId = Guid.Empty,
            clienteId,
            veiculoId,
            inicio = DateTime.UtcNow.AddDays(1),
            servicoIds,
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.TryGetProperty("errors", out _).Should().BeTrue();
    }

    [Fact]
    public async Task POST_sem_cliente_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var (filialId, _, veiculoId, servicoIds) = await SemearDependenciasAsync();

        var response = await client.PostAsJsonAsync(RotaCriar, new
        {
            filialId,
            clienteId = Guid.Empty,
            veiculoId,
            inicio = DateTime.UtcNow.AddDays(1),
            servicoIds,
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_veiculo_de_outro_cliente_retorna_400_RN002()
    {
        // RN002: o veículo informado pertence a outro titular, não ao clienteId enviado.
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var (filialId, _, _, servicoIds) = await SemearDependenciasAsync();
        var (_, clienteOutro, _, _) = await SemearDependenciasAsync();
        var (_, _, veiculoId, _) = await SemearDependenciasAsync();

        var response = await client.PostAsJsonAsync(RotaCriar, new
        {
            filialId,
            clienteId = clienteOutro,
            veiculoId,
            inicio = DateTime.UtcNow.AddDays(1),
            servicoIds,
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_mesmo_veiculo_mesma_filial_mesmo_horario_retorna_409_CA006()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var (filialId, clienteId, veiculoId, servicoIds) = await SemearDependenciasAsync();
        var inicio = DateTime.UtcNow.AddDays(2);

        var primeiro = await client.PostAsJsonAsync(RotaCriar, new
        {
            filialId,
            clienteId,
            veiculoId,
            inicio,
            servicoIds,
        }, _json);
        primeiro.StatusCode.Should().Be(HttpStatusCode.Created);

        var segundo = await client.PostAsJsonAsync(RotaCriar, new
        {
            filialId,
            clienteId,
            veiculoId,
            inicio = inicio.AddMinutes(10),
            servicoIds,
        }, _json);

        segundo.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var corpo = await segundo.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("type").GetString().Should().Contain("agendamento-conflito-veiculo");
    }

    [Fact]
    public async Task POST_mesmo_veiculo_filiais_diferentes_mesmo_horario_retorna_409_CA006()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var (filialA, clienteId, veiculoId, servicoIds) = await SemearDependenciasAsync();
        var filialB = await SemearFilialAsync();
        var inicio = DateTime.UtcNow.AddDays(3);

        var naFilialA = await client.PostAsJsonAsync(RotaCriar, new
        {
            filialId = filialA,
            clienteId,
            veiculoId,
            inicio,
            servicoIds,
        }, _json);
        naFilialA.StatusCode.Should().Be(HttpStatusCode.Created);

        var naFilialB = await client.PostAsJsonAsync(RotaCriar, new
        {
            filialId = filialB,
            clienteId,
            veiculoId,
            inicio = inicio.AddMinutes(5),
            servicoIds,
        }, _json);

        naFilialB.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var corpo = await naFilialB.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("type").GetString().Should().Contain("agendamento-conflito-veiculo");
    }

    [Fact]
    public async Task POST_filial_inexistente_retorna_404()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var (_, clienteId, veiculoId, servicoIds) = await SemearDependenciasAsync();

        var response = await client.PostAsJsonAsync(RotaCriar, new
        {
            filialId = Guid.NewGuid(),
            clienteId,
            veiculoId,
            inicio = DateTime.UtcNow.AddDays(1),
            servicoIds,
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_com_filial_inativa_retorna_422()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var (_, clienteId, veiculoId, servicoIds) = await SemearDependenciasAsync();
        var filialInativaId = await SemearFilialAsync(ativa: false);

        var response = await client.PostAsJsonAsync(RotaCriar, new
        {
            filialId = filialInativaId,
            clienteId,
            veiculoId,
            inicio = DateTime.UtcNow.AddDays(1),
            servicoIds,
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("type").GetString().Should().Contain("recurso-inativo");
    }

    [Fact]
    public async Task POST_janelas_adjacentes_mesmo_veiculo_passam()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var (filialId, clienteId, veiculoId, servicoIds) = await SemearDependenciasAsync();
        var inicio = DateTime.UtcNow.AddDays(4);

        var primeiro = await client.PostAsJsonAsync(RotaCriar, new
        {
            filialId,
            clienteId,
            veiculoId,
            inicio,
            servicoIds = new[] { servicoIds[0] },
        }, _json);
        primeiro.StatusCode.Should().Be(HttpStatusCode.Created);

        var corpoPrimeiro = await primeiro.Content.ReadFromJsonAsync<JsonElement>(_json);
        var fimPrimeiro = corpoPrimeiro.GetProperty("fim").GetDateTime();

        // Segundo começa exatamente quando o primeiro termina — janela [) não colide.
        var segundo = await client.PostAsJsonAsync(RotaCriar, new
        {
            filialId,
            clienteId,
            veiculoId,
            inicio = fimPrimeiro,
            servicoIds = new[] { servicoIds[0] },
        }, _json);

        segundo.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task POST_multiplos_servicos_retorna_201_com_totais_somados()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var (filialId, clienteId, veiculoId, servicoIds) = await SemearDependenciasAsync();
        var inicio = DateTime.UtcNow.AddDays(7);

        var response = await client.PostAsJsonAsync(RotaCriar, new
        {
            filialId,
            clienteId,
            veiculoId,
            inicio,
            servicoIds,
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);

        // fim = inicio + soma das durações; totais coerentes com os serviços semeados.
        var inicioResp = corpo.GetProperty("inicio").GetDateTime();
        var fimResp = corpo.GetProperty("fim").GetDateTime();
        int duracaoTotal = corpo.GetProperty("duracaoTotalMin").GetInt32();
        fimResp.Should().Be(inicioResp.AddMinutes(duracaoTotal));
        corpo.GetProperty("itens").GetArrayLength().Should().Be(servicoIds.Count);

        int soma = corpo.GetProperty("itens").EnumerateArray()
            .Sum(s => s.GetProperty("duracaoAplicada").GetInt32());
        duracaoTotal.Should().Be(soma);
    }

    [Fact]
    public async Task POST_veiculo_inexistente_retorna_404()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var (filialId, clienteId, _, servicoIds) = await SemearDependenciasAsync();

        var response = await client.PostAsJsonAsync(RotaCriar, new
        {
            filialId,
            clienteId,
            veiculoId = Guid.NewGuid(),
            inicio = DateTime.UtcNow.AddDays(1),
            servicoIds,
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_servico_inexistente_retorna_404()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var (filialId, clienteId, veiculoId, _) = await SemearDependenciasAsync();

        var response = await client.PostAsJsonAsync(RotaCriar, new
        {
            filialId,
            clienteId,
            veiculoId,
            inicio = DateTime.UtcNow.AddDays(1),
            servicoIds = new[] { Guid.NewGuid() },
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_veiculo_inativo_retorna_422()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var (filialId, _, _, servicoIds) = await SemearDependenciasAsync();
        var (clienteInativo, veiculoInativoId) = await SemearVeiculoInativoAsync();

        var response = await client.PostAsJsonAsync(RotaCriar, new
        {
            filialId,
            clienteId = clienteInativo,
            veiculoId = veiculoInativoId,
            inicio = DateTime.UtcNow.AddDays(1),
            servicoIds,
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("type").GetString().Should().Contain("recurso-inativo");
    }

    [Fact]
    public async Task POST_servico_inativo_retorna_422()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var (filialId, clienteId, veiculoId, _) = await SemearDependenciasAsync();
        var servicoInativoId = await SemearServicoInativoAsync();

        var response = await client.PostAsJsonAsync(RotaCriar, new
        {
            filialId,
            clienteId,
            veiculoId,
            inicio = DateTime.UtcNow.AddDays(1),
            servicoIds = new[] { servicoInativoId },
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task POST_inicio_no_passado_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var (filialId, clienteId, veiculoId, servicoIds) = await SemearDependenciasAsync();

        var response = await client.PostAsJsonAsync(RotaCriar, new
        {
            filialId,
            clienteId,
            veiculoId,
            inicio = DateTime.UtcNow.AddHours(-2),
            servicoIds,
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_lista_de_servicos_vazia_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var (filialId, clienteId, veiculoId, _) = await SemearDependenciasAsync();

        var response = await client.PostAsJsonAsync(RotaCriar, new
        {
            filialId,
            clienteId,
            veiculoId,
            inicio = DateTime.UtcNow.AddDays(1),
            servicoIds = Array.Empty<Guid>(),
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_servico_duplicado_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var (filialId, clienteId, veiculoId, servicoIds) = await SemearDependenciasAsync();

        var response = await client.PostAsJsonAsync(RotaCriar, new
        {
            filialId,
            clienteId,
            veiculoId,
            inicio = DateTime.UtcNow.AddDays(1),
            servicoIds = new[] { servicoIds[0], servicoIds[0] },
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_observacoes_acima_de_500_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var (filialId, clienteId, veiculoId, servicoIds) = await SemearDependenciasAsync();

        var response = await client.PostAsJsonAsync(RotaCriar, new
        {
            filialId,
            clienteId,
            veiculoId,
            inicio = DateTime.UtcNow.AddDays(1),
            servicoIds,
            observacoes = new string('x', 501),
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_500_nao_expoe_stack_trace()
    {
        // Erro genérico não tratado: mensagem neutra, sem stack trace nem nomes de tipo.
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var (filialId, clienteId, veiculoId, servicoIds) = await SemearDependenciasAsync();

        var response = await client.PostAsJsonAsync(RotaCriar, new
        {
            filialId,
            clienteId,
            veiculoId,
            inicio = DateTime.UtcNow.AddDays(1),
            servicoIds,
        }, _json);

        // O caminho feliz não deve retornar 500; este teste guarda contra regressão
        // de vazamento — qualquer 5xx deve trazer corpo neutro.
        if ((int)response.StatusCode >= 500)
        {
            string corpo = await response.Content.ReadAsStringAsync();
            corpo.Should().NotContain("at CarWash.");
            corpo.Should().NotContain("Exception:");
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }
    }

    [Fact]
    public async Task POST_concorrente_mesmo_veiculo_mesmo_horario_exatamente_um_201_e_um_409_CA006()
    {
        // Race condition RN011/CA006: dois POSTs simultâneos para o mesmo veículo
        // e janela sobreposta. A constraint EXCLUDE ex_ag_veiculo_janela garante
        // que apenas um persiste — o outro recebe 409.
        var clienteA = await AuthenticatedHttpClient.CreateAsync(_factory);
        var clienteB = await AuthenticatedHttpClient.CreateAsync(_factory);
        var (filialId, clienteId, veiculoId, servicoIds) = await SemearDependenciasAsync();
        var inicio = DateTime.UtcNow.AddDays(10);

        object Corpo(DateTime quando) => new
        {
            filialId,
            clienteId,
            veiculoId,
            inicio = quando,
            servicoIds = new[] { servicoIds[0] },
        };

        var t1 = clienteA.PostAsJsonAsync(RotaCriar, Corpo(inicio), _json);
        var t2 = clienteB.PostAsJsonAsync(RotaCriar, Corpo(inicio.AddMinutes(5)), _json);

        var respostas = await Task.WhenAll(t1, t2);

        int[] codigos = respostas.Select(r => (int)r.StatusCode).ToArray();
        string resumo = string.Join(", ", codigos);

        // O banco deve ter exatamente 1 agendamento para o veículo na janela —
        // invariante de dados garantida pela constraint EXCLUDE ex_ag_veiculo_janela.
        await using var db = NovoDbContext();
        (await db.Agendamentos.CountAsync(a => a.VeiculoId == veiculoId))
            .Should().Be(1, "a EXCLUDE garante no máximo 1 agendamento na janela (códigos HTTP: {0})", resumo);

        codigos.Count(c => c == 201).Should()
            .Be(1, "exatamente um POST concorrente deve vencer com 201 (códigos HTTP: {0})", resumo);
        codigos.Count(c => c == 409).Should()
            .Be(1, "o POST perdedor deve receber 409, não outro código (códigos HTTP: {0})", resumo);
    }

    [Fact]
    public async Task POST_falha_de_persistencia_nao_deixa_agendamento_orfao()
    {
        // Após um 409 (conflito do segundo POST), não pode restar agendamento,
        // item ou histórico órfão do agendamento rejeitado — rollback total.
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var (filialId, clienteId, veiculoId, servicoIds) = await SemearDependenciasAsync();
        var inicio = DateTime.UtcNow.AddDays(12);

        var primeiro = await client.PostAsJsonAsync(RotaCriar, new
        {
            filialId,
            clienteId,
            veiculoId,
            inicio,
            servicoIds,
        }, _json);
        primeiro.StatusCode.Should().Be(HttpStatusCode.Created);

        var segundo = await client.PostAsJsonAsync(RotaCriar, new
        {
            filialId,
            clienteId,
            veiculoId,
            inicio = inicio.AddMinutes(10),
            servicoIds,
        }, _json);
        segundo.StatusCode.Should().Be(HttpStatusCode.Conflict);

        await using var db = NovoDbContext();
        var agendamentos = await db.Agendamentos
            .Where(a => a.VeiculoId == veiculoId)
            .Select(a => a.Id)
            .ToListAsync();
        agendamentos.Should().HaveCount(1);

        // Nenhum item/histórico órfão: todo AgendamentoItem/Historico deve apontar
        // para um agendamento existente. A varredura conta só os verdadeiros órfãos
        // (FK pendente) — não dados de outros testes no banco compartilhado.
        (await db.AgendamentoItens.CountAsync(i => !db.Agendamentos.Any(a => a.Id == i.AgendamentoId)))
            .Should().Be(0);
        (await db.AgendamentoHistoricos.CountAsync(h => !db.Agendamentos.Any(a => a.Id == h.AgendamentoId)))
            .Should().Be(0);
    }

    private async Task<(Guid ClienteId, Guid VeiculoId)> SemearVeiculoInativoAsync()
    {
        await using var db = NovoDbContext();
        var cliente = ClienteValido();
        var veiculo = Veiculo.Criar(
            id: Guid.NewGuid(),
            clienteId: cliente.Id,
            placa: new Placa(GerarPlacaAleatoria()),
            modelo: "Gol",
            fabricante: "Volkswagen",
            cor: "Branco");
        veiculo.Inativar();

        db.Clientes.Add(cliente);
        db.Veiculos.Add(veiculo);
        await db.SaveChangesAsync();
        return (cliente.Id, veiculo.Id);
    }

    private async Task<Guid> SemearServicoInativoAsync()
    {
        await using var db = NovoDbContext();
        var servico = Servico.Criar(Guid.NewGuid(), $"Servico {Guid.NewGuid():N}"[..30], 25m, 20);
        servico.Inativar();

        db.Servicos.Add(servico);
        await db.SaveChangesAsync();
        return servico.Id;
    }

    private async Task<(Guid FilialId, Guid ClienteId, Guid VeiculoId, IReadOnlyList<Guid> ServicoIds)> SemearDependenciasAsync()
    {
        await using var db = NovoDbContext();

        var filial = Filial.Criar(Guid.NewGuid(), $"Filial {Guid.NewGuid():N}"[..30], CodigoTeste(), 4);
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

    private async Task<Guid> SemearFilialAsync(bool ativa = true)
    {
        await using var db = NovoDbContext();
        var filial = Filial.Criar(Guid.NewGuid(), $"Filial {Guid.NewGuid():N}"[..30], CodigoTeste(), 3);
        if (!ativa)
        {
            filial.Inativar();
        }

        db.Filiais.Add(filial);
        await db.SaveChangesAsync();
        return filial.Id;
    }

    private static string CodigoTeste() => $"F{Guid.NewGuid():N}"[..10].ToUpperInvariant();

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
