using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarWash.IntegrationTests.Collections;
using CarWash.IntegrationTests.Fixtures;
using CarWash.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CarWash.IntegrationTests.Endpoints.Responsaveis;

[Collection(nameof(PostgresCollection))]
public class CriarResponsavelEndpointTests : IAsyncDisposable
{
    private readonly CarWashWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public CriarResponsavelEndpointTests(PostgresFixture fixture)
    {
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    [Fact]
    public async Task POST_valido_retorna_201_com_Location()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var clienteId = await CriarClienteAsync(client);

        var rota = RotaCriar(clienteId);
        var response = await client.PostAsJsonAsync(rota, PayloadValido(), _json);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.OriginalString.Should().Contain($"/api/v1/clientes/{clienteId}/responsaveis/");

        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("id").GetGuid().Should().NotBeEmpty();
        corpo.GetProperty("clienteTitularId").GetGuid().Should().Be(clienteId);
        corpo.GetProperty("nome").GetString().Should().Be("João Silva");
        corpo.GetProperty("mensagem").GetString().Should().Be("Responsável cadastrado com sucesso.");
        corpo.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task POST_sem_token_retorna_401()
    {
        var client = _factory.CreateClient();
        var clienteId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync(RotaCriar(clienteId), PayloadValido(), _json);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_body_vazio_retorna_400_com_problem_details()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var clienteId = await CriarClienteAsync(client);

        var response = await client.PostAsJsonAsync(RotaCriar(clienteId), new { }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.TryGetProperty("errors", out _).Should().BeTrue();
    }

    [Fact]
    public async Task POST_documento_invalido_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var clienteId = await CriarClienteAsync(client);

        var payload = PayloadValido();
        payload["documento"] = "11111111111";

        var response = await client.PostAsJsonAsync(RotaCriar(clienteId), payload, _json);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_grau_vinculo_invalido_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var clienteId = await CriarClienteAsync(client);

        var payload = PayloadValido();
        payload["grauVinculo"] = "INVALIDO";

        var response = await client.PostAsJsonAsync(RotaCriar(clienteId), payload, _json);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_cliente_titular_inexistente_retorna_404()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var response = await client.PostAsJsonAsync(RotaCriar(Guid.NewGuid()), PayloadValido(), _json);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_documento_duplicado_retorna_409()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var clienteId = await CriarClienteAsync(client);

        var cpf = NovoCpfValido();
        var primeiroPayload = PayloadValido();
        primeiroPayload["documento"] = cpf;

        var rota = RotaCriar(clienteId);
        var primeiro = await client.PostAsJsonAsync(rota, primeiroPayload, _json);
        primeiro.StatusCode.Should().Be(HttpStatusCode.Created);

        var segundoPayload = PayloadValido();
        segundoPayload["documento"] = cpf;
        segundoPayload["email"] = $"outro-{Guid.NewGuid():N}@x.com";

        var segundo = await client.PostAsJsonAsync(rota, segundoPayload, _json);
        segundo.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var corpo = await segundo.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("type").GetString().Should().Contain("responsavel-documento-duplicado");
    }

    private static Uri RotaCriar(Guid clienteTitularId) =>
        new($"/api/v1/clientes/{clienteTitularId}/responsaveis", UriKind.Relative);

    private static Dictionary<string, object?> PayloadValido() => new()
    {
        ["nome"] = "João Silva",
        ["documento"] = NovoCpfValido(),
        ["telefone"] = "11987654321",
        ["email"] = $"joao-{Guid.NewGuid():N}@x.com",
        ["grauVinculo"] = "RESPONSAVEL_FINANCEIRO",
    };

    private async Task<Guid> CriarClienteAsync(HttpClient client)
    {
        var payload = new Dictionary<string, object?>
        {
            ["nome"] = $"Titular-{Guid.NewGuid():N}",
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
        response.StatusCode.Should().Be(HttpStatusCode.Created, $"falha ao criar cliente titular: {await response.Content.ReadAsStringAsync()}");

        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        return corpo.GetProperty("id").GetGuid();
    }

    private static string NovoCpfValido()
    {
        var rng = Random.Shared;
        var bases = new int[9];
        for (var i = 0; i < 9; i++)
        {
            bases[i] = rng.Next(0, 10);
        }

        if (bases.Distinct().Count() == 1)
        {
            bases[0] = (bases[0] + 1) % 10;
        }

        var d1 = DigitoVerificador(bases, 10);
        var d2 = DigitoVerificador([.. bases, d1], 11);
        return string.Concat(string.Concat(bases), d1, d2);
    }

    private static int DigitoVerificador(int[] digitos, int pesoInicial)
    {
        var soma = 0;
        for (var i = 0; i < digitos.Length; i++)
        {
            soma += digitos[i] * (pesoInicial - i);
        }

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
