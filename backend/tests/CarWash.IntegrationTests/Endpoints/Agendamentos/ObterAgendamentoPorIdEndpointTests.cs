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
/// Cobre o RF010 — <c>GET /api/v1/agendamentos/{id}</c> ponta a
/// ponta com Testcontainers + PostgreSQL real. Valida 200 e 404.
/// </summary>
[Collection(nameof(PostgresCollection))]
public class ObterAgendamentoPorIdEndpointTests : IAsyncDisposable
{
    private static readonly Uri RotaCriar = new("/api/v1/agendamentos", UriKind.Relative);

    private readonly CarWashWebApplicationFactory _factory;
    private readonly PostgresFixture _fixture;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public ObterAgendamentoPorIdEndpointTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    private static Uri RotaObterPorId(Guid id) => new($"/api/v1/agendamentos/{id}", UriKind.Relative);

    [Fact]
    public async Task GET_agendamento_existente_retorna_200_com_dados_completos()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var agendamentoId = await CriarAgendamentoAsync(client);

        var response = await client.GetAsync(RotaObterPorId(agendamentoId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("message").GetString().Should().Be("Agendamento encontrado.");
        var data = corpo.GetProperty("data");
        data.GetProperty("id").GetGuid().Should().Be(agendamentoId);
        data.GetProperty("status").GetString().Should().Be("agendado");
        data.GetProperty("filialId").GetGuid().Should().NotBeEmpty();
        data.GetProperty("clienteId").GetGuid().Should().NotBeEmpty();
        data.GetProperty("veiculoId").GetGuid().Should().NotBeEmpty();
        data.GetProperty("criadoEm").GetDateTime().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        data.TryGetProperty("itens", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GET_agendamento_inexistente_retorna_404()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var response = await client.GetAsync(RotaObterPorId(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_sem_token_retorna_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(RotaObterPorId(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<Guid> CriarAgendamentoAsync(HttpClient client)
    {
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
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        return corpo.GetProperty("id").GetGuid();
    }

    private async Task<(Guid FilialId, Guid ClienteId, Guid VeiculoId, IReadOnlyList<Guid> ServicoIds)>
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

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
