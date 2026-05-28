using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarWash.IntegrationTests.Collections;
using CarWash.IntegrationTests.Fixtures;
using CarWash.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace CarWash.IntegrationTests.Endpoints.Veiculos;

[Collection(nameof(PostgresCollection))]
public class CriarVeiculoEndpointTests : IAsyncDisposable
{
    private readonly CarWashWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public CriarVeiculoEndpointTests(PostgresFixture fixture)
    {
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    [Fact]
    public async Task POST_valido_retorna_201_com_Location()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var clienteId = await CriarClienteAsync(client);
        var placa = NovaPlaca();

        var response = await client.PostAsJsonAsync(Rota(clienteId), PayloadValido(placa), _json);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.OriginalString.Should().StartWith($"/api/v1/clientes/{clienteId}/veiculos/");

        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("id").GetGuid().Should().NotBeEmpty();
        corpo.GetProperty("placa").GetString().Should().Be(placa);
        corpo.GetProperty("clienteId").GetGuid().Should().Be(clienteId);
    }

    [Fact]
    public async Task POST_sem_token_retorna_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(Rota(Guid.NewGuid()), PayloadValido(), _json);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_body_vazio_retorna_400_com_problem_details()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var clienteId = await CriarClienteAsync(client);

        var response = await client.PostAsJsonAsync(Rota(clienteId), new { }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_placa_formato_invalido_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var clienteId = await CriarClienteAsync(client);

        var payload = PayloadValido();
        payload["placa"] = "INVALIDA-99";

        var response = await client.PostAsJsonAsync(Rota(clienteId), payload, _json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_cliente_inexistente_retorna_404()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var response = await client.PostAsJsonAsync(Rota(Guid.NewGuid()), PayloadValido(), _json);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_placa_duplicada_retorna_409_com_slug_canonico()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var clienteId = await CriarClienteAsync(client);

        var placa = NovaPlaca();
        var primeiro = await client.PostAsJsonAsync(Rota(clienteId), PayloadValido(placa), _json);
        primeiro.StatusCode.Should().Be(HttpStatusCode.Created);

        // Segundo cliente para garantir que a unicidade da placa é GLOBAL (RN011).
        var outroClienteId = await CriarClienteAsync(client);
        var segundo = await client.PostAsJsonAsync(Rota(outroClienteId), PayloadValido(placa), _json);

        segundo.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var corpo = await segundo.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("type").GetString().Should().Contain("placa-ja-cadastrada");
    }

    [Fact]
    public async Task POST_cliente_inativo_retorna_422()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var clienteId = await CriarClienteAsync(client);

        await InativarClienteAsync(client, clienteId);

        var response = await client.PostAsJsonAsync(Rota(clienteId), PayloadValido(NovaPlaca()), _json);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private static Uri Rota(Guid clienteId) => new($"/api/v1/clientes/{clienteId}/veiculos", UriKind.Relative);

    private async Task<Guid> CriarClienteAsync(HttpClient client)
    {
        var payload = new Dictionary<string, object?>
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
        };

        var response = await client.PostAsJsonAsync("/api/v1/clientes", payload, _json);
        response.EnsureSuccessStatusCode();
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        return corpo.GetProperty("id").GetGuid();
    }

    private async Task InativarClienteAsync(HttpClient client, Guid clienteId)
    {
        var response = await client.PatchAsJsonAsync(
            new Uri($"/api/v1/clientes/{clienteId}/status", UriKind.Relative),
            new { ativo = false },
            _json);
        response.EnsureSuccessStatusCode();
    }

    private static Dictionary<string, object?> PayloadValido(string? placa = null) => new()
    {
        ["placa"] = placa ?? "ABC1D23",
        ["modelo"] = "Onix",
        ["fabricante"] = "Chevrolet",
        ["cor"] = "Prata",
        ["ano"] = 2022,
    };

    private static string NovaPlaca()
    {
        // Formato Mercosul: 3 letras + 1 dígito + 1 letra + 2 dígitos.
        // Geramos randomicamente respeitando o regex
        // ^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$ — válido tanto no value object
        // Placa quanto no CHECK constraint ck_veiculos_placa_formato.
        var rng = Random.Shared;
        Span<char> chars = stackalloc char[7];
        chars[0] = (char)('A' + rng.Next(0, 26));
        chars[1] = (char)('A' + rng.Next(0, 26));
        chars[2] = (char)('A' + rng.Next(0, 26));
        chars[3] = (char)('0' + rng.Next(0, 10));
        chars[4] = (char)('A' + rng.Next(0, 26));
        chars[5] = (char)('0' + rng.Next(0, 10));
        chars[6] = (char)('0' + rng.Next(0, 10));
        return new string(chars);
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
