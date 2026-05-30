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

[Collection(nameof(PostgresCollection))]
public class CriarAgendamentoEndpointTests : IAsyncDisposable
{
    private static readonly Uri RotaCriar = new("/api/v1/agendamentos", UriKind.Relative);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly CarWashWebApplicationFactory _factory;
    private readonly PostgresFixture _fixture;

    public CriarAgendamentoEndpointTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    [Fact]
    public async Task POST_valido_retorna_201()
    {
        var (filialId, clienteId, veiculoId, servicoId) = await SemearDependenciasAsync();

        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var payload = PayloadValido(filialId, clienteId, veiculoId, servicoId);
        var response = await client.PostAsJsonAsync(RotaCriar, payload, Json);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        corpo.GetProperty("message").GetString().Should().Be("Agendamento criado com sucesso.");
        corpo.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();

        var data = corpo.GetProperty("data");
        data.GetProperty("filialId").GetGuid().Should().Be(filialId);
        data.GetProperty("clienteId").GetGuid().Should().Be(clienteId);
        data.GetProperty("veiculoId").GetGuid().Should().Be(veiculoId);
        data.GetProperty("status").GetString().Should().Be("AGENDADO");
        data.GetProperty("duracaoTotalMin").GetInt32().Should().Be(30);
        data.GetProperty("valorTotal").GetDecimal().Should().Be(30m);
    }

    [Fact]
    public async Task POST_sem_token_retorna_401()
    {
        var client = _factory.CreateClient();
        var payload = PayloadValido(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var response = await client.PostAsJsonAsync(RotaCriar, payload, Json);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_body_vazio_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var response = await client.PostAsJsonAsync(RotaCriar, new { }, Json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        corpo.TryGetProperty("errors", out _).Should().BeTrue();
    }

    [Fact]
    public async Task POST_filial_inexistente_retorna_404()
    {
        var (_, clienteId, veiculoId, servicoId) = await SemearDependenciasAsync();

        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var payload = PayloadValido(Guid.NewGuid(), clienteId, veiculoId, servicoId);
        var response = await client.PostAsJsonAsync(RotaCriar, payload, Json);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_veiculo_conflito_retorna_409()
    {
        var (filialId, clienteId, veiculoId, servicoId) = await SemearDependenciasAsync();

        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var inicio = DateTime.UtcNow.AddHours(2);
        var payload = PayloadValido(filialId, clienteId, veiculoId, servicoId, inicio);
        var primeiro = await client.PostAsJsonAsync(RotaCriar, payload, Json);
        primeiro.StatusCode.Should().Be(HttpStatusCode.Created);

        var segundo = await client.PostAsJsonAsync(RotaCriar, payload, Json);
        segundo.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var corpo = await segundo.Content.ReadFromJsonAsync<JsonElement>(Json);
        corpo.GetProperty("type").GetString().Should().Contain("veiculo-conflito");
    }

    [Fact]
    public async Task POST_capacidade_atingida_retorna_409()
    {
        var (filialId, clienteId, veiculoId, servicoId) = await SemearDependenciasComCapacidadeAsync(1);

        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var inicio = DateTime.UtcNow.AddHours(2);
        var payload1 = PayloadValido(filialId, clienteId, veiculoId, servicoId, inicio);
        var primeiro = await client.PostAsJsonAsync(RotaCriar, payload1, Json);
        primeiro.StatusCode.Should().Be(HttpStatusCode.Created);

        var outroVeiculoId = await SemearVeiculoAsync(clienteId);
        var payload2 = PayloadValido(filialId, clienteId, outroVeiculoId, servicoId, inicio);
        var segundo = await client.PostAsJsonAsync(RotaCriar, payload2, Json);
        segundo.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var corpo = await segundo.Content.ReadFromJsonAsync<JsonElement>(Json);
        corpo.GetProperty("type").GetString().Should().Contain("capacidade-filial");
    }

    private static Dictionary<string, object?> PayloadValido(
        Guid filialId, Guid clienteId, Guid veiculoId, Guid servicoId, DateTime? inicio = null) => new()
    {
        ["filialId"] = filialId,
        ["clienteId"] = clienteId,
        ["veiculoId"] = veiculoId,
        ["inicio"] = inicio ?? DateTime.UtcNow.AddHours(2),
        ["servicoIds"] = new[] { servicoId },
        ["observacoes"] = (string?)null,
    };

    private async Task<(Guid filialId, Guid clienteId, Guid veiculoId, Guid servicoId)> SemearDependenciasAsync()
    {
        return await SemearDependenciasComCapacidadeAsync(4);
    }

    private async Task<(Guid filialId, Guid clienteId, Guid veiculoId, Guid servicoId)> SemearDependenciasComCapacidadeAsync(
        int celulasAtivas)
    {
        await using var db = CarWashDbContextFactoryForTests.Create(_fixture);

        var filialId = Guid.NewGuid();
        var filial = Filial.Criar(filialId, $"Filial-{filialId:N}", celulasAtivas);
        db.Filiais.Add(filial);

        var clienteId = Guid.NewGuid();
        var cliente = Cliente.Criar(
            clienteId, "Maria Teste", new DateOnly(1990, 5, 15),
            new Telefone("11988887777"),
            new Endereco("12345678", "Rua Teste", "42", null, "Centro", "São Paulo", "SP"),
            cpf: new Cpf("52998224725"));
        db.Clientes.Add(cliente);

        var veiculoId = Guid.NewGuid();
        var veiculo = Veiculo.Criar(
            veiculoId, clienteId, new Placa(GerarPlacaValida()), "Corolla", "Toyota", "Branco", 2024);
        db.Veiculos.Add(veiculo);

        var servicoId = Guid.NewGuid();
        var servico = Servico.Criar(servicoId, $"Lavagem-{servicoId:N}", 30m, 30);
        db.Servicos.Add(servico);

        await db.SaveChangesAsync();

        return (filialId, clienteId, veiculoId, servicoId);
    }

    private async Task<Guid> SemearVeiculoAsync(Guid clienteId)
    {
        await using var db = CarWashDbContextFactoryForTests.Create(_fixture);

        var veiculoId = Guid.NewGuid();
        var veiculo = Veiculo.Criar(
            veiculoId, clienteId, new Placa(GerarPlacaValida()), "Civic", "Honda", "Preto", 2023);
        db.Veiculos.Add(veiculo);

        await db.SaveChangesAsync();
        return veiculoId;
    }

    private static string GerarPlacaValida()
    {
        var rand = Random.Shared;
        char L() => (char)('A' + rand.Next(26));
        char D() => (char)('0' + rand.Next(10));
        return $"{L()}{L()}{L()}{D()}{L()}{D()}{D()}";
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
