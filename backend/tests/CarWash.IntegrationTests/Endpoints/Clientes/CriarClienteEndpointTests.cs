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
public class CriarClienteEndpointTests : IAsyncDisposable
{
    private static readonly Uri RotaCriar = new("/api/v1/clientes", UriKind.Relative);

    private readonly CarWashWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public CriarClienteEndpointTests(PostgresFixture fixture)
    {
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    [Fact]
    public async Task POST_valido_retorna_201_com_Location()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var response = await client.PostAsJsonAsync(RotaCriar, PayloadValido(), _json);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.OriginalString.Should().StartWith("/api/v1/clientes/");

        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("id").GetGuid().Should().NotBeEmpty();
        corpo.GetProperty("mensagem").GetString().Should().Be("Cliente cadastrado com sucesso.");
        corpo.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task POST_sem_token_retorna_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(RotaCriar, PayloadValido(), _json);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_body_vazio_retorna_400_com_problem_details()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var response = await client.PostAsJsonAsync(RotaCriar, new { }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.TryGetProperty("errors", out _).Should().BeTrue();
    }

    [Fact]
    public async Task POST_cpf_invalido_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var payload = PayloadValido();
        payload["cpf"] = "11111111111";

        var response = await client.PostAsJsonAsync(RotaCriar, payload, _json);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_cpf_duplicado_retorna_409()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var cpf = NovoCpfValido();
        var primeiroPayload = PayloadValido();
        primeiroPayload["cpf"] = cpf;

        var primeiro = await client.PostAsJsonAsync(RotaCriar, primeiroPayload, _json);
        primeiro.StatusCode.Should().Be(HttpStatusCode.Created);

        // Mesmo CPF, e-mail diferente para garantir conflito por documento.
        var segundoPayload = PayloadValido();
        segundoPayload["cpf"] = cpf;
        segundoPayload["email"] = $"outro-{Guid.NewGuid():N}@x.com";
        var segundo = await client.PostAsJsonAsync(RotaCriar, segundoPayload, _json);

        segundo.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var corpo = await segundo.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("type").GetString().Should().Contain("cliente-documento-duplicado");
    }

    private static Dictionary<string, object?> PayloadValido() => new()
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

    /// <summary>
    /// Gera um CPF válido por <c>Random</c> usando o algoritmo de dígitos
    /// verificadores — evita colisão entre testes que rodam contra o mesmo
    /// container Postgres da fixture.
    /// </summary>
    private static string NovoCpfValido()
    {
        var rng = Random.Shared;
        var bases = new int[9];
        for (var i = 0; i < 9; i++)
        {
            bases[i] = rng.Next(0, 10);
        }

        // Reject sequências triviais (00000..., 11111...) que falham o validator.
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
