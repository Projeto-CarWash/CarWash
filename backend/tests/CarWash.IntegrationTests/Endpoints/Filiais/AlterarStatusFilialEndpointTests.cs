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

namespace CarWash.IntegrationTests.Endpoints.Filiais;

/// <summary>
/// Cobre o RF017 — <c>PATCH /api/v1/filiais/{id}/status</c> (ativar/inativar)
/// — e o desdobramento no RF019: criação de agendamento com filial inativa
/// retorna 409 <c>filial-inativa</c>.
/// </summary>
[Collection(nameof(PostgresCollection))]
public class AlterarStatusFilialEndpointTests : IAsyncDisposable
{
    private static readonly Uri RotaCriarAgendamento = new("/api/v1/agendamentos", UriKind.Relative);

    private readonly CarWashWebApplicationFactory _factory;
    private readonly PostgresFixture _fixture;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public AlterarStatusFilialEndpointTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    private static Uri RotaStatus(Guid id) => new($"/api/v1/filiais/{id}/status", UriKind.Relative);

    [Fact]
    public async Task PATCH_status_inativa_retorna_200_com_ativa_false()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var filialId = await SemearFilialAsync();

        var response = await client.PatchAsJsonAsync(RotaStatus(filialId), new { ativo = false }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("ativa").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task PATCH_status_reativa_retorna_200_com_ativa_true()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var filialId = await SemearFilialAsync();

        (await client.PatchAsJsonAsync(RotaStatus(filialId), new { ativo = false }, _json))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await client.PatchAsJsonAsync(RotaStatus(filialId), new { ativo = true }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("ativa").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task PATCH_status_sem_campo_ativo_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var filialId = await SemearFilialAsync();

        var response = await client.PatchAsJsonAsync(RotaStatus(filialId), new { }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PATCH_status_filial_inexistente_retorna_404()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var response = await client.PatchAsJsonAsync(RotaStatus(Guid.NewGuid()), new { ativo = false }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_agendamento_com_filial_inativada_via_api_retorna_409()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var (filialId, clienteId, veiculoId, responsavelId, servicoIds) = await SemearDependenciasAsync();

        (await client.PatchAsJsonAsync(RotaStatus(filialId), new { ativo = false }, _json))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // RF019: filial inativa não é aceita para novos agendamentos.
        var response = await client.PostAsJsonAsync(RotaCriarAgendamento, new
        {
            filialId,
            clienteId,
            veiculoId,
            responsavelId,
            inicio = DateTime.UtcNow.AddDays(1),
            servicoIds,
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("type").GetString().Should().Contain("filial-inativa");
    }

    private async Task<Guid> SemearFilialAsync()
    {
        await using var db = NovoDbContext();
        var filial = Filial.Criar(Guid.NewGuid(), $"Filial {Guid.NewGuid():N}"[..30], $"F{Guid.NewGuid():N}"[..10].ToUpperInvariant(), 4);
        db.Filiais.Add(filial);
        await db.SaveChangesAsync();
        return filial.Id;
    }

    private async Task<(Guid FilialId, Guid ClienteId, Guid VeiculoId, Guid ResponsavelId, IReadOnlyList<Guid> ServicoIds)>
        SemearDependenciasAsync()
    {
        await using var db = NovoDbContext();

        var filial = Filial.Criar(Guid.NewGuid(), $"Filial {Guid.NewGuid():N}"[..30], $"F{Guid.NewGuid():N}"[..10].ToUpperInvariant(), 4);
        var cliente = Cliente.Criar(
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
