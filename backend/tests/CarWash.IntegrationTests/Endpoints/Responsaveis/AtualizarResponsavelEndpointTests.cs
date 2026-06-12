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
/// Cobre o RF023/RF024 — <c>PUT /api/v1/clientes/{clienteId}/responsaveis/{id}</c>
/// (atualização cadastral; documento imutável) e
/// <c>PATCH .../{id}/status</c> (ativar/inativar).
/// </summary>
[Collection(nameof(PostgresCollection))]
public class AtualizarResponsavelEndpointTests : IAsyncDisposable
{
    private readonly CarWashWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public AtualizarResponsavelEndpointTests(PostgresFixture fixture)
    {
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    private static Uri RotaCriar(Guid clienteId) =>
        new($"/api/v1/clientes/{clienteId}/responsaveis", UriKind.Relative);

    private static Uri RotaAtualizar(Guid clienteId, Guid id) =>
        new($"/api/v1/clientes/{clienteId}/responsaveis/{id}", UriKind.Relative);

    private static Uri RotaStatus(Guid clienteId, Guid id) =>
        new($"/api/v1/clientes/{clienteId}/responsaveis/{id}/status", UriKind.Relative);

    [Fact]
    public async Task PUT_atualiza_dados_e_mantem_documento_imutavel()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var clienteId = await CriarClienteAsync(client);
        var (responsavelId, documento) = await CriarResponsavelAsync(client, clienteId);

        var response = await client.PutAsJsonAsync(
            RotaAtualizar(clienteId, responsavelId),
            new
            {
                nome = "Maria Atualizada",
                telefone = "11912345678",
                email = "maria@x.com",
                grauVinculo = "PROCURADOR",
                documento = "99999999999",
            },
            _json);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("responsavelId").GetGuid().Should().Be(responsavelId);
        corpo.GetProperty("nome").GetString().Should().Be("Maria Atualizada");
        corpo.GetProperty("telefone").GetString().Should().Be("11912345678");
        corpo.GetProperty("grauVinculo").GetString().Should().Be("PROCURADOR");
        corpo.GetProperty("documento").GetString().Should().Be(documento, "documento é imutável no PUT");
    }

    [Fact]
    public async Task PUT_responsavel_de_outro_cliente_retorna_404()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var clienteA = await CriarClienteAsync(client);
        var clienteB = await CriarClienteAsync(client);
        var (responsavelDeA, _) = await CriarResponsavelAsync(client, clienteA);

        var response = await client.PutAsJsonAsync(
            RotaAtualizar(clienteB, responsavelDeA),
            new { nome = "Tentativa Cruzada", grauVinculo = "OUTRO" },
            _json);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_nome_invalido_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var clienteId = await CriarClienteAsync(client);
        var (responsavelId, _) = await CriarResponsavelAsync(client, clienteId);

        var response = await client.PutAsJsonAsync(
            RotaAtualizar(clienteId, responsavelId),
            new { nome = "ab", grauVinculo = "OUTRO" },
            _json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PATCH_status_inativa_e_reativa_responsavel()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var clienteId = await CriarClienteAsync(client);
        var (responsavelId, _) = await CriarResponsavelAsync(client, clienteId);

        var inativar = await client.PatchAsJsonAsync(
            RotaStatus(clienteId, responsavelId), new { ativo = false }, _json);
        inativar.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpoInativo = await inativar.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpoInativo.GetProperty("ativo").GetBoolean().Should().BeFalse();

        var reativar = await client.PatchAsJsonAsync(
            RotaStatus(clienteId, responsavelId), new { ativo = true }, _json);
        reativar.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpoAtivo = await reativar.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpoAtivo.GetProperty("ativo").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task PATCH_status_sem_campo_ativo_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var clienteId = await CriarClienteAsync(client);
        var (responsavelId, _) = await CriarResponsavelAsync(client, clienteId);

        var response = await client.PatchAsJsonAsync(
            RotaStatus(clienteId, responsavelId), new { }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<(Guid Id, string Documento)> CriarResponsavelAsync(HttpClient client, Guid clienteId)
    {
        string documento = NovoCpfValido();
        var response = await client.PostAsJsonAsync(RotaCriar(clienteId), new Dictionary<string, object?>
        {
            ["nome"] = "João Silva",
            ["documento"] = documento,
            ["telefone"] = "11987654321",
            ["email"] = $"joao-{Guid.NewGuid():N}@x.com",
            ["grauVinculo"] = "RESPONSAVEL_FINANCEIRO",
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.Created, $"falha ao criar responsável: {await response.Content.ReadAsStringAsync()}");
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        return (corpo.GetProperty("data").GetProperty("responsavelId").GetGuid(), documento);
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
        response.StatusCode.Should().Be(HttpStatusCode.Created, $"falha ao criar cliente titular: {await response.Content.ReadAsStringAsync()}");

        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        return corpo.GetProperty("id").GetGuid();
    }

    private static string NovoCpfValido()
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
