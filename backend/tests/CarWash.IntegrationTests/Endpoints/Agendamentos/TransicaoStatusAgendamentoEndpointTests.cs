using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarWash.Domain.Entities;
using CarWash.Domain.Enums;
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
/// Cobre o ciclo de atendimento (RF010/RF013) — <c>PATCH /iniciar</c> e
/// <c>PATCH /finalizar</c> — e a tradução do conflito RN011 no caminho de
/// edição (RF020: editar horário para faixa com conflito → 409, não 500;
/// edição na própria janela → self-check correto, 200).
/// </summary>
[Collection(nameof(PostgresCollection))]
public class TransicaoStatusAgendamentoEndpointTests : IAsyncDisposable
{
    private static readonly Uri RotaCriar = new("/api/v1/agendamentos", UriKind.Relative);

    private readonly CarWashWebApplicationFactory _factory;
    private readonly PostgresFixture _fixture;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public TransicaoStatusAgendamentoEndpointTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    private static Uri RotaIniciar(Guid id) => new($"/api/v1/agendamentos/{id}/iniciar", UriKind.Relative);

    private static Uri RotaFinalizar(Guid id) => new($"/api/v1/agendamentos/{id}/finalizar", UriKind.Relative);

    private static Uri RotaEditar(Guid id) => new($"/api/v1/agendamentos/{id}", UriKind.Relative);

    [Fact]
    public async Task PATCH_iniciar_agendado_retorna_200_em_andamento()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var contexto = await CriarAgendamentoAsync(client, DateTime.UtcNow.AddDays(1));

        var response = await client.PatchAsJsonAsync(RotaIniciar(contexto.AgendamentoId), new { }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("message").GetString().Should().Be("Atendimento iniciado com sucesso.");
        corpo.GetProperty("data").GetProperty("status").GetString().Should().Be("em_andamento");
    }

    [Fact]
    public async Task PATCH_finalizar_em_andamento_retorna_200_finalizado()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var contexto = await CriarAgendamentoAsync(client, DateTime.UtcNow.AddDays(1));

        (await client.PatchAsJsonAsync(RotaIniciar(contexto.AgendamentoId), new { }, _json))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await client.PatchAsJsonAsync(RotaFinalizar(contexto.AgendamentoId), new { }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("data").GetProperty("status").GetString().Should().Be("finalizado");
    }

    [Fact]
    public async Task PATCH_finalizar_agendado_sem_iniciar_retorna_409()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var contexto = await CriarAgendamentoAsync(client, DateTime.UtcNow.AddDays(1));

        var response = await client.PatchAsJsonAsync(RotaFinalizar(contexto.AgendamentoId), new { }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("type").GetString().Should().Contain("agendamento-transicao-status");
    }

    [Fact]
    public async Task PATCH_iniciar_inexistente_retorna_404()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var response = await client.PatchAsJsonAsync(RotaIniciar(Guid.NewGuid()), new { }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PATCH_iniciar_ja_em_andamento_retorna_409()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var contexto = await CriarAgendamentoAsync(client, DateTime.UtcNow.AddDays(1));

        (await client.PatchAsJsonAsync(RotaIniciar(contexto.AgendamentoId), new { }, _json))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await client.PatchAsJsonAsync(RotaIniciar(contexto.AgendamentoId), new { }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PATCH_finalizado_libera_celula_e_janela_do_veiculo()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var inicio = DateTime.UtcNow.AddDays(2);
        var contexto = await CriarAgendamentoAsync(client, inicio);

        (await client.PatchAsJsonAsync(RotaIniciar(contexto.AgendamentoId), new { }, _json))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PatchAsJsonAsync(RotaFinalizar(contexto.AgendamentoId), new { }, _json))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // RF008: agendamento CONCLUIDO não ocupa vaga nem bloqueia a janela do
        // veículo — novo agendamento no mesmo horário deve ser aceito.
        var response = await client.PostAsJsonAsync(RotaCriar, new
        {
            filialId = contexto.FilialId,
            clienteId = contexto.ClienteId,
            veiculoId = contexto.VeiculoId,
            responsavelId = contexto.ResponsavelId,
            inicio,
            servicoIds = contexto.ServicoIds,
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PATCH_editar_horario_para_faixa_com_conflito_retorna_409()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var inicioA = DateTime.UtcNow.AddDays(1);
        var contexto = await CriarAgendamentoAsync(client, inicioA);

        // Segundo agendamento do MESMO veículo em outra janela (sem conflito).
        var inicioB = inicioA.AddHours(6);
        var responseB = await client.PostAsJsonAsync(RotaCriar, new
        {
            filialId = contexto.FilialId,
            clienteId = contexto.ClienteId,
            veiculoId = contexto.VeiculoId,
            responsavelId = contexto.ResponsavelId,
            inicio = inicioB,
            servicoIds = contexto.ServicoIds,
        }, _json);
        responseB.StatusCode.Should().Be(HttpStatusCode.Created);
        var corpoB = await responseB.Content.ReadFromJsonAsync<JsonElement>(_json);
        var agendamentoB = corpoB.GetProperty("id").GetGuid();

        // RF020: mover B para a janela de A deve devolver 409 (não 500).
        var (inicioConflito, fimConflito) = await JanelaDoAgendamentoAsync(contexto.AgendamentoId);
        var response = await client.PatchAsJsonAsync(
            RotaEditar(agendamentoB),
            new { inicio = inicioConflito, fim = fimConflito },
            _json);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("type").GetString().Should().Contain("agendamento-conflito-veiculo");
    }

    [Fact]
    public async Task PATCH_editar_horario_na_propria_janela_nao_conflita_consigo_mesmo()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var contexto = await CriarAgendamentoAsync(client, DateTime.UtcNow.AddDays(1));

        // Self-check (RF020): deslocar levemente dentro da própria janela —
        // sobrepõe a versão anterior do MESMO agendamento e deve ter sucesso.
        var (inicioAtual, fimAtual) = await JanelaDoAgendamentoAsync(contexto.AgendamentoId);
        var response = await client.PatchAsJsonAsync(
            RotaEditar(contexto.AgendamentoId),
            new { inicio = inicioAtual.AddMinutes(10), fim = fimAtual.AddMinutes(10) },
            _json);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<(DateTime Inicio, DateTime Fim)> JanelaDoAgendamentoAsync(Guid agendamentoId)
    {
        await using var db = NovoDbContext();
        var agendamento = await db.Agendamentos.AsNoTracking()
            .FirstAsync(a => a.Id == agendamentoId);
        return (agendamento.Inicio, agendamento.Fim);
    }

    private sealed record ContextoAgendamento(
        Guid AgendamentoId,
        Guid FilialId,
        Guid ClienteId,
        Guid VeiculoId,
        Guid ResponsavelId,
        IReadOnlyList<Guid> ServicoIds);

    private async Task<ContextoAgendamento> CriarAgendamentoAsync(HttpClient client, DateTime inicio)
    {
        var (filialId, clienteId, veiculoId, responsavelId, servicoIds) = await SemearDependenciasAsync();

        var response = await client.PostAsJsonAsync(RotaCriar, new
        {
            filialId,
            clienteId,
            veiculoId,
            responsavelId,
            inicio,
            servicoIds,
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        return new ContextoAgendamento(
            corpo.GetProperty("id").GetGuid(),
            filialId,
            clienteId,
            veiculoId,
            responsavelId,
            servicoIds);
    }

    private async Task<(Guid FilialId, Guid ClienteId, Guid VeiculoId, Guid ResponsavelId, IReadOnlyList<Guid> ServicoIds)>
        SemearDependenciasAsync()
    {
        await using var db = NovoDbContext();

        var filial = Filial.Criar(Guid.NewGuid(), $"Filial {Guid.NewGuid():N}"[..30], $"F{Guid.NewGuid():N}"[..10].ToUpperInvariant(), 4);
        var cliente = ClienteValido();
        var veiculo = Veiculo.Criar(
            id: Guid.NewGuid(),
            clienteId: cliente.Id,
            placa: new Placa(GerarPlacaAleatoria()),
            modelo: "Civic",
            fabricante: "Honda",
            cor: "Preto");
        var responsavel = Responsavel.Criar(
            id: Guid.NewGuid(),
            clienteTitularId: cliente.Id,
            nome: "Responsavel Teste",
            documento: GerarCpfValido(),
            grauVinculo: GrauVinculo.ResponsavelFinanceiro);

        db.Filiais.Add(filial);
        db.Clientes.Add(cliente);
        db.Veiculos.Add(veiculo);
        db.Responsaveis.Add(responsavel);
        await db.SaveChangesAsync();

        var servicoIds = await db.Servicos
            .AsNoTracking()
            .Where(s => s.Ativo)
            .OrderBy(s => s.Nome)
            .Select(s => s.Id)
            .Take(2)
            .ToListAsync();

        return (filial.Id, cliente.Id, veiculo.Id, responsavel.Id, servicoIds);
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

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
