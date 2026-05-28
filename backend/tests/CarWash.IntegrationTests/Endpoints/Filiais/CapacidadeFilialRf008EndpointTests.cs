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

namespace CarWash.IntegrationTests.Endpoints.Filiais;

/// <summary>
/// RF018 + RF008 — capacidade efetiva da filial (RN009). <c>celulas_ativas</c> é o
/// teto de agendamentos simultâneos numa janela. Como a EXCLUDE bloqueia o MESMO
/// veículo na mesma janela, a capacidade é exercitada com VEÍCULOS DISTINTOS no
/// mesmo horário. Também cobre o efeito do PATCH (reduzir/aumentar) sobre novas
/// tentativas — CA5/CA6 do card.
/// </summary>
[Collection(nameof(PostgresCollection))]
public class CapacidadeFilialRf008EndpointTests : IAsyncDisposable
{
    private static readonly Uri RotaFiliais = new("/api/v1/filiais", UriKind.Relative);
    private static readonly Uri RotaAgendamentos = new("/api/v1/agendamentos", UriKind.Relative);

    private readonly CarWashWebApplicationFactory _factory;
    private readonly PostgresFixture _fixture;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public CapacidadeFilialRf008EndpointTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    [Fact]
    public async Task Capacidade_2_aceita_dois_simultaneos_e_terceiro_estoura_409_com_slug()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var filialId = await CriarFilialAsync(client, celulas: 2);
        var servicoIds = await ObterServicoIdsAsync();
        var inicio = DateTime.UtcNow.AddDays(20);

        // Veículos distintos para não esbarrar na EXCLUDE de veículo — exercita a capacidade.
        var v1 = await SemearClienteVeiculoAsync();
        var v2 = await SemearClienteVeiculoAsync();
        var v3 = await SemearClienteVeiculoAsync();

        (await Agendar(client, filialId, v1, inicio, servicoIds)).StatusCode
            .Should().Be(HttpStatusCode.Created);
        (await Agendar(client, filialId, v2, inicio.AddMinutes(5), servicoIds)).StatusCode
            .Should().Be(HttpStatusCode.Created);

        var terceiro = await Agendar(client, filialId, v3, inicio.AddMinutes(10), servicoIds);
        terceiro.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var corpo = await terceiro.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("title").GetString()
            .Should().Be("Capacidade da filial esgotada para o horário solicitado.");
        corpo.GetProperty("type").GetString().Should().Contain("capacidade-filial-esgotada");
    }

    [Fact]
    public async Task Reduzir_para_1_preserva_existentes_e_bloqueia_novo_aumentar_libera()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var filialId = await CriarFilialAsync(client, celulas: 2);
        var servicoIds = await ObterServicoIdsAsync();
        var inicio = DateTime.UtcNow.AddDays(25);

        var v1 = await SemearClienteVeiculoAsync();
        var v2 = await SemearClienteVeiculoAsync();
        var v3 = await SemearClienteVeiculoAsync();
        var v4 = await SemearClienteVeiculoAsync();

        // Capacidade 2: dois agendamentos sobrepostos passam.
        (await Agendar(client, filialId, v1, inicio, servicoIds)).StatusCode
            .Should().Be(HttpStatusCode.Created);
        (await Agendar(client, filialId, v2, inicio.AddMinutes(5), servicoIds)).StatusCode
            .Should().Be(HttpStatusCode.Created);

        // Reduzir células ativas para 1 → 200. Não apaga os agendamentos já criados.
        var patchReduzir = await client.PatchAsJsonAsync(RotaCelulas(filialId), new { celulasAtivas = 1 }, _json);
        patchReduzir.StatusCode.Should().Be(HttpStatusCode.OK);

        await using (var db = CarWashDbContextFactoryForTests.Create(_fixture))
        {
            var preservados = await db.Agendamentos
                .AsNoTracking()
                .CountAsync(a => a.FilialId == filialId && a.StatusRaw == "agendado");
            preservados.Should().Be(2, "reduzir capacidade não pode apagar agendamentos existentes");
        }

        // Já há 2 simultâneos > 1 (novo teto) → nova tentativa na janela estoura 409.
        var bloqueado = await Agendar(client, filialId, v3, inicio.AddMinutes(8), servicoIds);
        bloqueado.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await bloqueado.Content.ReadFromJsonAsync<JsonElement>(_json))
            .GetProperty("type").GetString().Should().Contain("capacidade-filial-esgotada");

        // Aumentar para 5 → 200; agora um novo agendamento na mesma janela passa.
        var patchAumentar = await client.PatchAsJsonAsync(RotaCelulas(filialId), new { celulasAtivas = 5 }, _json);
        patchAumentar.StatusCode.Should().Be(HttpStatusCode.OK);

        var liberado = await Agendar(client, filialId, v4, inicio.AddMinutes(12), servicoIds);
        liberado.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private async Task<HttpResponseMessage> Agendar(
        HttpClient client,
        Guid filialId,
        (Guid ClienteId, Guid VeiculoId) v,
        DateTime inicio,
        IReadOnlyList<Guid> servicoIds) =>
        await client.PostAsJsonAsync(RotaAgendamentos, new
        {
            filialId,
            clienteId = v.ClienteId,
            veiculoId = v.VeiculoId,
            inicio,
            servicoIds = new[] { servicoIds[0] },
        }, _json);

    private async Task<Guid> CriarFilialAsync(HttpClient client, int celulas)
    {
        var nome = $"Filial {Guid.NewGuid():N}"[..30];

        // POST de filial agora exige `codigo` (regex ^[A-Z0-9]{2,20}$, único por filial)
        // após a reconciliação com a development.
        var codigo = $"F{Guid.NewGuid():N}"[..8].ToUpperInvariant();
        var response = await client.PostAsJsonAsync(RotaFiliais, new { nome, codigo, celulasAtivas = celulas }, _json);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>(_json)).GetProperty("id").GetGuid();
    }

    private async Task<IReadOnlyList<Guid>> ObterServicoIdsAsync()
    {
        await using var db = CarWashDbContextFactoryForTests.Create(_fixture);
        return await db.Servicos
            .AsNoTracking()
            .Where(s => s.Ativo)
            .OrderBy(s => s.Nome)
            .Select(s => s.Id)
            .Take(1)
            .ToListAsync();
    }

    private async Task<(Guid ClienteId, Guid VeiculoId)> SemearClienteVeiculoAsync()
    {
        await using var db = CarWashDbContextFactoryForTests.Create(_fixture);
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

    private static Uri RotaCelulas(Guid id) =>
        new($"/api/v1/filiais/{id}/celulas-ativas", UriKind.Relative);

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
        for (var i = 0; i < 9; i++)
        {
            d[i] = rng.Next(0, 10);
        }

        d[9] = Dv(d[..9], 10);
        d[10] = Dv(d[..10], 11);
        var chars = new char[11];
        for (var i = 0; i < 11; i++)
        {
            chars[i] = (char)('0' + d[i]);
        }

        return new string(chars);

        static int Dv(ReadOnlySpan<int> parcial, int pesoInicial)
        {
            var soma = 0;
            for (var i = 0; i < parcial.Length; i++)
            {
                soma += parcial[i] * (pesoInicial - i);
            }

            var resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
