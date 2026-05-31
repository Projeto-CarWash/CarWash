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

namespace CarWash.IntegrationTests.Endpoints.Agendamentos;

/// <summary>
/// Cobre o RF010 — <c>PATCH /api/v1/agendamentos/{id}/cancelar</c> ponta a
/// ponta com Testcontainers + PostgreSQL real. Valida o checklist QA do card 137
/// (200, 400, 401, 404, 409).
/// </summary>
[Collection(nameof(PostgresCollection))]
public class CancelarAgendamentoEndpointTests : IAsyncDisposable
{
	private static readonly Uri RotaCriar = new("/api/v1/agendamentos", UriKind.Relative);

	private readonly CarWashWebApplicationFactory _factory;
	private readonly PostgresFixture _fixture;
	private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

	public CancelarAgendamentoEndpointTests(PostgresFixture fixture)
	{
		_fixture = fixture;
		_factory = new CarWashWebApplicationFactory(fixture);
	}

	private Uri RotaCancelar(Guid id) => new($"/api/v1/agendamentos/{id}/cancelar", UriKind.Relative);

	[Fact]
	public async Task PATCH_cancelar_agendado_retorna_200_com_campos_do_contrato()
	{
		var client = await AuthenticatedHttpClient.CreateAsync(_factory);
		var agendamentoId = await CriarAgendamentoAsync(client);

		var response = await client.PatchAsJsonAsync(
			RotaCancelar(agendamentoId),
			new { motivoCancelamento = "Cliente solicitou cancelamento", origem = "TESTE" },
			_json);

		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
	 corpo.GetProperty("message").GetString().Should().Be("Agendamento cancelado com sucesso.");
		var data = corpo.GetProperty("data");
		data.GetProperty("id").GetGuid().Should().Be(agendamentoId);
		data.GetProperty("status").GetString().Should().Be("cancelado");
		data.GetProperty("canceladoPor").GetGuid().Should().NotBeEmpty();
		data.GetProperty("canceladoEm").GetDateTime().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
		data.GetProperty("motivoCancelamento").GetString().Should().Be("Cliente solicitou cancelamento");
	}

	[Fact]
	public async Task PATCH_sem_token_retorna_401()
	{
		var client = _factory.CreateClient();
		var id = Guid.NewGuid();

		var response = await client.PatchAsJsonAsync(
			RotaCancelar(id),
			new { motivoCancelamento = "Qualquer motivo", origem = "TESTE" },
			_json);

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task PATCH_agendamento_inexistente_retorna_404()
	{
		var client = await AuthenticatedHttpClient.CreateAsync(_factory);

		var response = await client.PatchAsJsonAsync(
			RotaCancelar(Guid.NewGuid()),
			new { motivoCancelamento = "Agendamento não existe", origem = "TESTE" },
			_json);

		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task PATCH_motivo_vazio_retorna_400()
	{
		var client = await AuthenticatedHttpClient.CreateAsync(_factory);
		var agendamentoId = await CriarAgendamentoAsync(client);

		var response = await client.PatchAsJsonAsync(
			RotaCancelar(agendamentoId),
			new { motivoCancelamento = "", origem = "TESTE" },
			_json);

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task PATCH_motivo_menor_que_5_retorna_400()
	{
		var client = await AuthenticatedHttpClient.CreateAsync(_factory);
		var agendamentoId = await CriarAgendamentoAsync(client);

		var response = await client.PatchAsJsonAsync(
			RotaCancelar(agendamentoId),
			new { motivoCancelamento = "abc", origem = "TESTE" },
			_json);

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task PATCH_motivo_maior_que_500_retorna_400()
	{
		var client = await AuthenticatedHttpClient.CreateAsync(_factory);
		var agendamentoId = await CriarAgendamentoAsync(client);

		var response = await client.PatchAsJsonAsync(
			RotaCancelar(agendamentoId),
			new { motivoCancelamento = new string('x', 501), origem = "TESTE" },
			_json);

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task PATCH_agendamento_finalizado_retorna_409()
	{
		var client = await AuthenticatedHttpClient.CreateAsync(_factory);
		var agendamentoId = await CriarAgendamentoAsync(client);
		await TransicionarViaDbAsync(agendamentoId, StatusAgendamento.Finalizado);

		var response = await client.PatchAsJsonAsync(
			RotaCancelar(agendamentoId),
			new { motivoCancelamento = "Tentativa de cancelar finalizado", origem = "TESTE" },
			_json);

		response.StatusCode.Should().Be(HttpStatusCode.Conflict);
		var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
		corpo.GetProperty("type").GetString().Should().Contain("agendamento-cancelamento-status");
		corpo.GetProperty("title").GetString().Should().Contain("finalizado");
	}

	[Fact]
	public async Task PATCH_agendamento_ja_cancelado_retorna_409()
	{
		var client = await AuthenticatedHttpClient.CreateAsync(_factory);
		var agendamentoId = await CriarAgendamentoAsync(client);

		var primeira = await client.PatchAsJsonAsync(
			RotaCancelar(agendamentoId),
			new { motivoCancelamento = "Primeiro cancelamento", origem = "TESTE" },
			_json);
		primeira.StatusCode.Should().Be(HttpStatusCode.OK);

		var segunda = await client.PatchAsJsonAsync(
			RotaCancelar(agendamentoId),
			new { motivoCancelamento = "Segundo cancelamento", origem = "TESTE" },
			_json);
		segunda.StatusCode.Should().Be(HttpStatusCode.Conflict);

		var corpo = await segunda.Content.ReadFromJsonAsync<JsonElement>(_json);
		corpo.GetProperty("type").GetString().Should().Contain("agendamento-cancelamento-status");
		corpo.GetProperty("title").GetString().Should().Contain("já cancelado");
	}

	[Fact]
	public async Task PATCH_agendamento_em_andamento_retorna_409()
	{
		var client = await AuthenticatedHttpClient.CreateAsync(_factory);
		var agendamentoId = await CriarAgendamentoAsync(client);
		await TransicionarViaDbAsync(agendamentoId, StatusAgendamento.EmAndamento);

		var response = await client.PatchAsJsonAsync(
			RotaCancelar(agendamentoId),
			new { motivoCancelamento = "Tentativa de cancelar em andamento", origem = "TESTE" },
			_json);

		response.StatusCode.Should().Be(HttpStatusCode.Conflict);
		var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
		corpo.GetProperty("type").GetString().Should().Contain("agendamento-cancelamento-status");
		corpo.GetProperty("title").GetString().Should().Contain("em andamento");
	}

	[Fact]
	public async Task PATCH_corpo_ausente_retorna_400()
	{
		var client = await AuthenticatedHttpClient.CreateAsync(_factory);
		var agendamentoId = await CriarAgendamentoAsync(client);

		var response = await client.PatchAsync(
			RotaCancelar(agendamentoId),
			new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json"));

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task PATCH_cancelar_grava_historico_no_banco()
	{
		var client = await AuthenticatedHttpClient.CreateAsync(_factory);
		var agendamentoId = await CriarAgendamentoAsync(client);

		await client.PatchAsJsonAsync(
			RotaCancelar(agendamentoId),
			new { motivoCancelamento = "Verificar histórico de auditoria", origem = "TESTE" },
			_json);

		await using var db = NovoDbContext();
		var historicos = await db.AgendamentoHistoricos
			.Where(h => h.AgendamentoId == agendamentoId)
			.ToListAsync();
		historicos.Should().Contain(h => h.Evento == EventoHistorico.Cancelado);
	}

	private async Task<Guid> CriarAgendamentoAsync(HttpClient client)
	{
		var (filialId, clienteId, veiculoId, servicoIds) = await SemearDependenciasAsync();

		var response = await client.PostAsJsonAsync(RotaCriar, new
		{
			filialId,
			clienteId,
			veiculoId,
			inicio = DateTime.UtcNow.AddDays(1),
			servicoIds,
		}, _json);

		response.StatusCode.Should().Be(HttpStatusCode.Created);
		var corpo = await response.Content.ReadFromJsonAsync<JsonElement>(_json);
		return corpo.GetProperty("id").GetGuid();
	}

	private async Task TransicionarViaDbAsync(Guid agendamentoId, StatusAgendamento novoStatus)
	{
		await using var db = NovoDbContext();
		var agendamento = await db.Agendamentos.FindAsync(agendamentoId);
		agendamento.Should().NotBeNull();

		if (novoStatus == StatusAgendamento.EmAndamento)
		{
			agendamento!.Iniciar();
		}
		else if (novoStatus == StatusAgendamento.Finalizado)
		{
			agendamento!.Iniciar();
			agendamento.Finalizar();
		}

		await db.SaveChangesAsync();
	}

	private async Task<(Guid FilialId, Guid ClienteId, Guid VeiculoId, IReadOnlyList<Guid> ServicoIds)>
		SemearDependenciasAsync()
	{
		await using var db = NovoDbContext();

		var filial = Filial.Criar(Guid.NewGuid(), $"Filial {Guid.NewGuid():N}"[..30], 4);
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
