using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarWash.IntegrationTests.Collections;
using CarWash.IntegrationTests.Fixtures;
using CarWash.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CarWash.IntegrationTests.Endpoints.Responsaveis;

/// <summary>
/// RF023/RF024 — <c>GET /api/v1/clientes/{id}/responsaveis</c>. Antes da correção
/// só existia POST, então o GET devolvia 405 e o dropdown de responsável (RF024)
/// ficava vazio. Estes testes garantem o contrato consumido pelo frontend.
/// </summary>
[Collection(nameof(PostgresCollection))]
public class ListarResponsaveisEndpointTests : IAsyncDisposable
{
    private readonly CarWashWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public ListarResponsaveisEndpointTests(PostgresFixture fixture)
    {
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    [Fact]
    public async Task GET_lista_responsaveis_do_cliente_retorna_200_com_itens()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var clienteId = await CriarClienteAsync(client);
        await CriarResponsavelAsync(client, clienteId, "Ana Responsavel");
        await CriarResponsavelAsync(client, clienteId, "Bruno Responsavel");

        var response = await client.GetAsync(Rota(clienteId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var itens = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        itens.ValueKind.Should().Be(JsonValueKind.Array);
        itens.GetArrayLength().Should().Be(2);

        var primeiro = itens[0];
        primeiro.GetProperty("id").GetGuid().Should().NotBeEmpty();
        primeiro.GetProperty("nome").GetString().Should().NotBeNullOrWhiteSpace();
        primeiro.GetProperty("documento").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GET_cliente_sem_responsaveis_retorna_200_lista_vazia()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var clienteId = await CriarClienteAsync(client);

        var response = await client.GetAsync(Rota(clienteId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var itens = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        itens.ValueKind.Should().Be(JsonValueKind.Array);
        itens.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GET_sem_token_retorna_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(Rota(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static Uri Rota(Guid clienteTitularId) =>
        new($"/api/v1/clientes/{clienteTitularId}/responsaveis", UriKind.Relative);

    private async Task CriarResponsavelAsync(HttpClient client, Guid clienteId, string nome)
    {
        var payload = new Dictionary<string, object?>
        {
            ["nome"] = nome,
            ["documento"] = NovoCpfValido(),
            ["telefone"] = "11987654321",
            ["email"] = $"resp-{Guid.NewGuid():N}@x.com",
            ["grauVinculo"] = "RESPONSAVEL_FINANCEIRO",
        };

        var response = await client.PostAsJsonAsync(Rota(clienteId), payload, _json);
        response.StatusCode.Should().Be(
            HttpStatusCode.Created,
            $"falha ao criar responsável: {await response.Content.ReadAsStringAsync()}");
    }

    private async Task<Guid> CriarClienteAsync(HttpClient client)
    {
        var payload = new Dictionary<string, object?>
        {
            ["nome"] = "Cliente Titular",
            ["dataNascimento"] = "1990-01-01",
            ["cpf"] = NovoCpfValido(),
            ["celular"] = "11987654321",
            ["email"] = $"titular-{Guid.NewGuid():N}@x.com",
            ["endereco"] = new
            {
                cep = "01001000",
                logradouro = "Praça da Sé",
                numero = "1",
                bairro = "Sé",
                cidade = "São Paulo",
                uf = "SP",
            },
        };

        var response = await client.PostAsJsonAsync(new Uri("/api/v1/clientes", UriKind.Relative), payload, _json);
        response.StatusCode.Should().Be(
            HttpStatusCode.Created,
            $"falha ao criar cliente titular: {await response.Content.ReadAsStringAsync()}");

        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        return corpo.GetProperty("id").GetGuid();
    }

    private static string NovoCpfValido()
    {
        var rng = Random.Shared;
        int[] bases = new int[9];
        for (int i = 0; i < 9; i++)
        {
            bases[i] = rng.Next(0, 10);
        }

        if (bases.Distinct().Count() == 1)
        {
            bases[0] = (bases[0] + 1) % 10;
        }

        int d1 = DigitoVerificador(bases, 10);
        int d2 = DigitoVerificador([.. bases, d1], 11);
        return string.Concat(string.Concat(bases), d1, d2);
    }

    private static int DigitoVerificador(int[] numeros, int pesoInicial)
    {
        int soma = 0;
        int peso = pesoInicial;
        foreach (int n in numeros)
        {
            soma += n * peso;
            peso--;
        }

        int resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
