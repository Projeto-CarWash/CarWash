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
public class AlterarStatusClienteEndpointTests : IAsyncDisposable
{
    private static readonly Uri RotaCriar = new("/api/v1/clientes", UriKind.Relative);

    private readonly CarWashWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public AlterarStatusClienteEndpointTests(PostgresFixture fixture)
    {
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    [Fact]
    public async Task PATCH_ativo_false_inativa_cliente_e_GET_confirma()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var id = await CadastrarAsync(client);

        var resp = await client.PatchAsJsonAsync(RotaStatus(id), new { ativo = false }, _json);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var corpo = await resp.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("id").GetGuid().Should().Be(id);
        corpo.GetProperty("ativo").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task PATCH_body_vazio_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var id = await CadastrarAsync(client);

        var requisicao = new HttpRequestMessage(HttpMethod.Patch, RotaStatus(id))
        {
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json"),
        };
        var resp = await client.SendAsync(requisicao);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PATCH_id_inexistente_retorna_404()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);

        var resp = await client.PatchAsJsonAsync(RotaStatus(Guid.NewGuid()), new { ativo = false }, _json);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static Uri RotaStatus(Guid id) =>
        new($"/api/v1/clientes/{id}/status", UriKind.Relative);

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
