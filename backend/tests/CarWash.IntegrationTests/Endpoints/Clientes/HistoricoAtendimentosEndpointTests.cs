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

namespace CarWash.IntegrationTests.Endpoints.Clientes;

/// <summary>
/// Cobre o RF012 — <c>GET /api/v1/clientes/{clienteId}/historico-atendimentos</c>.
/// Garante que o retorno lista os agendamentos do cliente (não apenas o total),
/// com responsável populado, ordenação do mais recente primeiro e vocabulário
/// de status do contrato (CONCLUIDO para atendimento finalizado).
/// </summary>
[Collection(nameof(PostgresCollection))]
public class HistoricoAtendimentosEndpointTests : IAsyncDisposable
{
    private static readonly Uri RotaCriarAgendamento = new("/api/v1/agendamentos", UriKind.Relative);

    private readonly CarWashWebApplicationFactory _factory;
    private readonly PostgresFixture _fixture;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public HistoricoAtendimentosEndpointTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    private static Uri RotaHistorico(Guid clienteId) =>
        new($"/api/v1/clientes/{clienteId}/historico-atendimentos", UriKind.Relative);

    [Fact]
    public async Task GET_cliente_com_agendamentos_retorna_lista_com_dados_e_ordenacao_desc()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var seed = await SemearDependenciasAsync();

        var inicioAntigo = DateTime.UtcNow.AddDays(1);
        var inicioRecente = DateTime.UtcNow.AddDays(3);
        var idAntigo = await CriarAgendamentoAsync(client, seed, inicioAntigo);
        var idRecente = await CriarAgendamentoAsync(client, seed, inicioRecente);

        var response = await client.GetAsync(RotaHistorico(seed.ClienteId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);

        corpo.GetProperty("meta").GetProperty("total").GetInt32().Should().Be(2);

        var data = corpo.GetProperty("data");
        data.GetArrayLength().Should().Be(2, "o histórico deve listar os agendamentos, não apenas o total");

        // Ordenação padrão: evento mais recente primeiro.
        data[0].GetProperty("agendamentoId").GetGuid().Should().Be(idRecente);
        data[1].GetProperty("agendamentoId").GetGuid().Should().Be(idAntigo);

        // Responsável vem da tabela responsaveis (FK do agendamento).
        data[0].GetProperty("usuarioResponsavel").GetProperty("nome").GetString()
            .Should().Be("Responsavel Historico");

        data[0].GetProperty("servicos").GetArrayLength().Should().BeGreaterThan(0);
        data[0].GetProperty("status").GetString().Should().Be("AGENDADO");
    }

    [Fact]
    public async Task GET_filtro_status_CONCLUIDO_retorna_atendimento_finalizado()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var seed = await SemearDependenciasAsync();
        var agendamentoId = await CriarAgendamentoAsync(client, seed, DateTime.UtcNow.AddDays(1));

        (await client.PatchAsJsonAsync(
            new Uri($"/api/v1/agendamentos/{agendamentoId}/iniciar", UriKind.Relative), new { }, _json))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PatchAsJsonAsync(
            new Uri($"/api/v1/agendamentos/{agendamentoId}/finalizar", UriKind.Relative), new { }, _json))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await client.GetAsync(
            new Uri($"/api/v1/clientes/{seed.ClienteId}/historico-atendimentos?status=CONCLUIDO", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);

        corpo.GetProperty("meta").GetProperty("total").GetInt32().Should().Be(1);
        var data = corpo.GetProperty("data");
        data.GetArrayLength().Should().Be(1);
        data[0].GetProperty("agendamentoId").GetGuid().Should().Be(agendamentoId);
        data[0].GetProperty("status").GetString().Should().Be("CONCLUIDO");
        data[0].GetProperty("concluidoEm").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task GET_cliente_inexistente_retorna_404()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var response = await client.GetAsync(RotaHistorico(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record SeedHistorico(
        Guid FilialId,
        Guid ClienteId,
        Guid VeiculoId,
        Guid ResponsavelId,
        IReadOnlyList<Guid> ServicoIds);

    private async Task<Guid> CriarAgendamentoAsync(HttpClient client, SeedHistorico seed, DateTime inicio)
    {
        var response = await client.PostAsJsonAsync(RotaCriarAgendamento, new
        {
            filialId = seed.FilialId,
            clienteId = seed.ClienteId,
            veiculoId = seed.VeiculoId,
            responsavelId = seed.ResponsavelId,
            inicio,
            servicoIds = seed.ServicoIds,
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        return corpo.GetProperty("id").GetGuid();
    }

    private async Task<SeedHistorico> SemearDependenciasAsync()
    {
        await using var db = CarWashDbContextFactoryForTests.Create(_fixture);

        var filial = Filial.Criar(Guid.NewGuid(), $"Filial {Guid.NewGuid():N}"[..30], $"F{Guid.NewGuid():N}"[..10].ToUpperInvariant(), 4);
        var cliente = Cliente.Criar(
            id: Guid.NewGuid(),
            nome: "Cliente Historico",
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
            nome: "Responsavel Historico",
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

        return new SeedHistorico(filial.Id, cliente.Id, veiculo.Id, responsavel.Id, servicoIds);
    }

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
