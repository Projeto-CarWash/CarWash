using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarWash.IntegrationTests.Collections;
using CarWash.IntegrationTests.Fixtures;
using CarWash.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CarWash.IntegrationTests.Endpoints.Clientes;

[Collection(nameof(PostgresCollection))]
public class AtualizarClienteEndpointTests : IAsyncDisposable
{
    private static readonly Uri RotaCriar = new("/api/v1/clientes", UriKind.Relative);

    private readonly CarWashWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public AtualizarClienteEndpointTests(PostgresFixture fixture)
    {
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    [Fact]
    public async Task PUT_atualiza_dados_retorna_200()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var id = await CadastrarAsync(client);

        var payload = PayloadAtualizacao();
        payload["nome"] = "Maria Atualizada";

        var response = await client.PutAsJsonAsync(new Uri($"/api/v1/clientes/{id}", UriKind.Relative), payload, _json);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("id").GetGuid().Should().Be(id);
        corpo.GetProperty("nome").GetString().Should().Be("Maria Atualizada");
    }

    [Fact]
    public async Task PUT_id_inexistente_retorna_404()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var response = await client.PutAsJsonAsync(
            new Uri($"/api/v1/clientes/{Guid.NewGuid()}", UriKind.Relative),
            PayloadAtualizacao(),
            _json);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_payload_invalido_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var id = await CadastrarAsync(client);

        var response = await client.PutAsJsonAsync(
            new Uri($"/api/v1/clientes/{id}", UriKind.Relative),
            new { },
            _json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<Guid> CadastrarAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(RotaCriar, new Dictionary<string, object?>
        {
            ["nome"] = "Maria Souza",
            ["dataNascimento"] = "1990-01-01",
            ["cpf"] = NovoCpfValido(),
            ["celular"] = "11987654321",
            ["email"] = $"maria-{Guid.NewGuid():N}@x.com",
            ["endereco"] = new
            {
                cep = "01001000",
                logradouro = "Praça da Sé",
                numero = "1",
                bairro = "Sé",
                cidade = "São Paulo",
                uf = "SP",
            },
        }, _json);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        return corpo.GetProperty("id").GetGuid();
    }

    private static Dictionary<string, object?> PayloadAtualizacao() => new()
    {
        ["nome"] = "Maria Souza",
        ["dataNascimento"] = "1990-01-01",
        ["celular"] = "11987654321",
        ["email"] = $"maria-{Guid.NewGuid():N}@x.com",
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

    private static int DigitoVerificador(int[] digitos, int pesoInicial)
    {
        int soma = 0;
        for (int i = 0; i < digitos.Length; i++)
        {
            soma += digitos[i] * (pesoInicial - i);
        }

        int resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
