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

/// <summary>
/// Cobre o RF015 — etapa 1 (<c>POST /api/v1/agendamentos/pre-confirmacao</c>):
/// gera o resumo e o <c>tokenConfirmacao</c> sem persistir. Itens 1 e 9 do
/// checklist do card 133 + os caminhos de erro 404/422 herdados do RF007.
/// </summary>
[Collection(nameof(PostgresCollection))]
public class PreConfirmarAgendamentoEndpointTests : IAsyncDisposable
{
    private static readonly Uri RotaPreConfirmar =
        new("/api/v1/agendamentos/pre-confirmacao", UriKind.Relative);

    private readonly CarWashWebApplicationFactory _factory;
    private readonly PostgresFixture _fixture;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public PreConfirmarAgendamentoEndpointTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _factory = new CarWashWebApplicationFactory(fixture);
    }

    /// <summary>Checklist 1: pré-confirmação válida devolve o resumo completo SEM persistir.</summary>
    [Fact]
    public async Task POST_valido_retorna_200_com_resumo_e_token_sem_persistir()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var (filialId, clienteId, veiculoId, servicoIds) = await SemearDependenciasAsync();
        var inicio = DateTime.UtcNow.AddDays(1);

        int idempotenciaAntes;
        await using (var dbAntes = NovoDbContext())
        {
            idempotenciaAntes = await dbAntes.IdempotenciaRequisicoes.CountAsync();
        }

        var response = await client.PostAsJsonAsync(RotaPreConfirmar, new
        {
            filialId,
            clienteId,
            veiculoId,
            inicio,
            servicoIds,
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("tokenConfirmacao").GetString().Should().NotBeNullOrWhiteSpace();
        corpo.GetProperty("expiraEm").GetDateTime().Should().BeAfter(DateTime.UtcNow);

        var resumo = corpo.GetProperty("resumo");
        resumo.GetProperty("filial").GetProperty("id").GetGuid().Should().Be(filialId);
        resumo.GetProperty("cliente").GetProperty("id").GetGuid().Should().Be(clienteId);
        resumo.GetProperty("veiculo").GetProperty("id").GetGuid().Should().Be(veiculoId);
        resumo.GetProperty("servicos").GetArrayLength().Should().Be(servicoIds.Count);
        resumo.GetProperty("duracaoTotalMin").GetInt32().Should().BeGreaterThan(0);
        resumo.GetProperty("valorTotal").GetDecimal().Should().BeGreaterThan(0m);
        resumo.GetProperty("hashResumo").GetString().Should().NotBeNullOrWhiteSpace();

        // O cerne do item 1: nada foi gravado pela pré-confirmação. A contagem de
        // idempotência usa delta antes/depois — o banco de integração é compartilhado.
        await using var db = NovoDbContext();
        (await db.Agendamentos.CountAsync(a => a.VeiculoId == veiculoId)).Should().Be(0);
        (await db.IdempotenciaRequisicoes.CountAsync()).Should()
            .Be(idempotenciaAntes, "a pré-confirmação não pode criar registros de idempotência");
    }

    /// <summary>O token devolvido tem o formato esperado de duas partes base64url.</summary>
    [Fact]
    public async Task POST_valido_token_tem_formato_de_duas_partes()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var (filialId, clienteId, veiculoId, servicoIds) = await SemearDependenciasAsync();

        var response = await client.PostAsJsonAsync(RotaPreConfirmar, new
        {
            filialId,
            clienteId,
            veiculoId,
            inicio = DateTime.UtcNow.AddDays(1),
            servicoIds,
        }, _json);

        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        var token = corpo.GetProperty("tokenConfirmacao").GetString()!;
        token.Split('.').Should().HaveCount(2);
        token.Should().NotContain("+").And.NotContain("/").And.NotContain("=");
    }

    /// <summary>Checklist 9: sem autenticação a pré-confirmação retorna 401.</summary>
    [Fact]
    public async Task POST_sem_token_de_acesso_retorna_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(RotaPreConfirmar, new { }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>Pré-confirmação sem filial é barrada pelo validator estrutural (CA007).</summary>
    [Fact]
    public async Task POST_sem_filial_retorna_400()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var (_, clienteId, veiculoId, servicoIds) = await SemearDependenciasAsync();

        var response = await client.PostAsJsonAsync(RotaPreConfirmar, new
        {
            filialId = Guid.Empty,
            clienteId,
            veiculoId,
            inicio = DateTime.UtcNow.AddDays(1),
            servicoIds,
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>Filial inexistente referenciada na prévia retorna 404.</summary>
    [Fact]
    public async Task POST_filial_inexistente_retorna_404()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var (_, clienteId, veiculoId, servicoIds) = await SemearDependenciasAsync();

        var response = await client.PostAsJsonAsync(RotaPreConfirmar, new
        {
            filialId = Guid.NewGuid(),
            clienteId,
            veiculoId,
            inicio = DateTime.UtcNow.AddDays(1),
            servicoIds,
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// RF019/card 142: filial inativa na prévia retorna 409 com slug filial-inativa
    /// e a mensagem exata do card — não mais 422.
    /// </summary>
    [Fact]
    public async Task POST_filial_inativa_retorna_409_com_slug_filial_inativa()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var (_, clienteId, veiculoId, servicoIds) = await SemearDependenciasAsync();
        var filialInativaId = await SemearFilialAsync(ativa: false);

        var response = await client.PostAsJsonAsync(RotaPreConfirmar, new
        {
            filialId = filialInativaId,
            clienteId,
            veiculoId,
            inicio = DateTime.UtcNow.AddDays(1),
            servicoIds,
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("type").GetString().Should().Contain("filial-inativa");
        corpo.GetProperty("title").GetString().Should()
            .Be("A filial selecionada está inativa e não pode receber novos agendamentos.");
    }

    /// <summary>RF019: filial inexistente na prévia traz a mensagem exata do card.</summary>
    [Fact]
    public async Task POST_filial_inexistente_retorna_404_com_mensagem_do_card()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var (_, clienteId, veiculoId, servicoIds) = await SemearDependenciasAsync();

        var response = await client.PostAsJsonAsync(RotaPreConfirmar, new
        {
            filialId = Guid.NewGuid(),
            clienteId,
            veiculoId,
            inicio = DateTime.UtcNow.AddDays(1),
            servicoIds,
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("title").GetString().Should().Be("Filial não encontrada.");
    }

    /// <summary>RF019: filialId ausente na prévia traz a mensagem do card em errors[filialId].</summary>
    [Fact]
    public async Task POST_sem_filialId_retorna_400_com_mensagem_do_card()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var (_, clienteId, veiculoId, servicoIds) = await SemearDependenciasAsync();

        var response = await client.PostAsJsonAsync(RotaPreConfirmar, new
        {
            filialId = Guid.Empty,
            clienteId,
            veiculoId,
            inicio = DateTime.UtcNow.AddDays(1),
            servicoIds,
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("errors").GetProperty("filialId").EnumerateArray()
            .Select(m => m.GetString())
            .Should().Contain("Selecione uma filial válida para prosseguir.");
    }

    /// <summary>
    /// L9 do RF015: o conflito de veículo (RN011) é detectado já na prévia — 409
    /// com o slug <c>agendamento-conflito-veiculo</c>.
    /// </summary>
    [Fact]
    public async Task POST_veiculo_com_conflito_de_horario_retorna_409()
    {
        var client = await AuthenticatedHttpClient.CreateAsync(_factory);
        var (filialId, clienteId, veiculoId, servicoIds) = await SemearDependenciasAsync();
        var inicio = DateTime.UtcNow.AddDays(5);

        // Cria um agendamento real ocupando a janela do veículo.
        var criar = await client.PostAsJsonAsync(
            new Uri("/api/v1/agendamentos", UriKind.Relative),
            new { filialId, clienteId, veiculoId, inicio, servicoIds },
            _json);
        criar.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await client.PostAsJsonAsync(RotaPreConfirmar, new
        {
            filialId,
            clienteId,
            veiculoId,
            inicio = inicio.AddMinutes(10),
            servicoIds,
        }, _json);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
        corpo.GetProperty("type").GetString().Should().Contain("agendamento-conflito-veiculo");
    }

    private async Task<(Guid FilialId, Guid ClienteId, Guid VeiculoId, IReadOnlyList<Guid> ServicoIds)>
        SemearDependenciasAsync()
    {
        await using var db = NovoDbContext();

        var filial = Filial.Criar(Guid.NewGuid(), $"Filial {Guid.NewGuid():N}"[..30], CodigoTeste(), 4);
        var cliente = ClienteValido();
        var veiculo = Veiculo.Criar(
            id: Guid.NewGuid(),
            clienteId: cliente.Id,
            placa: new Placa(GerarPlacaAleatoria()),
            modelo: "Civic",
            fabricante: "Honda",
            cor: "Preto");

        db.Filiais.Add(filial);
        db.Clientes.Add(cliente);
        db.Veiculos.Add(veiculo);
        await db.SaveChangesAsync();

        var servicoIds = await db.Servicos
            .AsNoTracking()
            .Where(s => s.Ativo)
            .OrderBy(s => s.Nome)
            .Select(s => s.Id)
            .Take(2)
            .ToListAsync();

        return (filial.Id, cliente.Id, veiculo.Id, servicoIds);
    }

    private async Task<Guid> SemearFilialAsync(bool ativa = true)
    {
        await using var db = NovoDbContext();
        var filial = Filial.Criar(Guid.NewGuid(), $"Filial {Guid.NewGuid():N}"[..30], CodigoTeste(), 3);
        if (!ativa)
        {
            filial.Inativar();
        }

        db.Filiais.Add(filial);
        await db.SaveChangesAsync();
        return filial.Id;
    }

    private static string CodigoTeste() => $"F{Guid.NewGuid():N}"[..10].ToUpperInvariant();

    private CarWashDbContext NovoDbContext() => CarWashDbContextFactoryForTests.Create(_fixture);

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
        return $"{letras[rng.Next(26)]}{letras[rng.Next(26)]}{letras[rng.Next(26)]}{rng.Next(0, 10)}{letras[rng.Next(26)]}{rng.Next(0, 10)}{rng.Next(0, 10)}";
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
