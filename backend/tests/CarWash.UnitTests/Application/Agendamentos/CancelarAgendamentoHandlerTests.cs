using CarWash.Application.Agendamentos.Cancelar;
using CarWash.Application.Agendamentos.Common;
using CarWash.Application.Agendamentos.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Domain.Common;
using CarWash.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace CarWash.UnitTests.Application.Agendamentos;

public class CancelarAgendamentoHandlerTests
{
	private static readonly Guid AgendamentoId = Guid.NewGuid();
	private static readonly Guid UsuarioId = Guid.NewGuid();

	private readonly IAgendamentoRepository _agendamentos = Substitute.For<IAgendamentoRepository>();

	private CancelarAgendamentoHandler NovoHandler() => new(_agendamentos, NullLogger<CancelarAgendamentoHandler>.Instance);

	private CancelarAgendamentoCommand NovoComando(string motivo = "Cliente solicitou cancelamento") =>
		new(AgendamentoId, motivo, "USUARIO_INTERNO", "trace-1", UsuarioId);

	private Agendamento NovoAgendamento()
	{
		var ag = Agendamento.Criar(
			id: AgendamentoId,
			filialId: Guid.NewGuid(),
			clienteId: Guid.NewGuid(),
			veiculoId: Guid.NewGuid(),
			criadoPor: UsuarioId,
			inicio: DateTime.UtcNow.AddHours(1),
			fim: DateTime.UtcNow.AddHours(2));
		return ag;
	}

	[Fact]
	public async Task Caminho_feliz_cancela_agendamento_agendado()
	{
		var agendamento = NovoAgendamento();
		_agendamentos.ObterPorIdRastreadoAsync(AgendamentoId, Arg.Any<CancellationToken>())
			.Returns(agendamento);

		var handler = NovoHandler();
		var resposta = await handler.HandleAsync(NovoComando(), CancellationToken.None);

		resposta.Message.Should().Be("Agendamento cancelado com sucesso.");
		resposta.Data.Id.Should().Be(AgendamentoId);
		resposta.Data.Status.Should().Be("cancelado");
		resposta.Data.CanceladoPor.Should().Be(UsuarioId);
		resposta.Data.MotivoCancelamento.Should().Be("Cliente solicitou cancelamento");
		resposta.Data.CanceladoEm.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

		await _agendamentos.Received(1).SalvarAsync(
			Arg.Is(agendamento),
			Arg.Is<AgendamentoHistorico>(h => h.AgendamentoId == AgendamentoId),
			"trace-1",
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Agendamento_nao_encontrado_lanca_NotFoundException()
	{
		_agendamentos.ObterPorIdRastreadoAsync(AgendamentoId, Arg.Any<CancellationToken>())
			.Returns((Agendamento?)null);

		var handler = NovoHandler();
		var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

		await act.Should().ThrowAsync<NotFoundException>();
	}

	[Fact]
	public async Task Usuario_nao_autenticado_lanca_ValidationException()
	{
		var handler = NovoHandler();
		var command = new CancelarAgendamentoCommand(AgendamentoId, "Motivo válido", "USUARIO_INTERNO", "trace-1", null);

		var act = () => handler.HandleAsync(command, CancellationToken.None);
		await act.Should().ThrowAsync<ValidationException>();
	}

	[Fact]
	public async Task Agendamento_finalizado_lanca_CancelamentoStatusException()
	{
        var agendamento = NovoAgendamento();
        agendamento.Iniciar();
        agendamento.Finalizar();
		_agendamentos.ObterPorIdRastreadoAsync(AgendamentoId, Arg.Any<CancellationToken>())
			.Returns(agendamento);

		var handler = NovoHandler();
		var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

		var ex = await act.Should().ThrowAsync<CancelamentoStatusException>();
		ex.Which.Message.Should().Contain("finalizado");
	}

	[Fact]
	public async Task Agendamento_cancelado_lanca_CancelamentoStatusException()
	{
		var agendamento = NovoAgendamento();
		agendamento.Cancelar("Primeiro cancelamento", UsuarioId);
		_agendamentos.ObterPorIdRastreadoAsync(AgendamentoId, Arg.Any<CancellationToken>())
			.Returns(agendamento);

		var handler = NovoHandler();
		var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

		var ex = await act.Should().ThrowAsync<CancelamentoStatusException>();
		ex.Which.Message.Should().Contain("já cancelado");
	}

	[Fact]
	public async Task Agendamento_em_andamento_lanca_CancelamentoStatusException()
	{
		var agendamento = NovoAgendamento();
		agendamento.Iniciar();
		_agendamentos.ObterPorIdRastreadoAsync(AgendamentoId, Arg.Any<CancellationToken>())
			.Returns(agendamento);

		var handler = NovoHandler();
		var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

		var ex = await act.Should().ThrowAsync<CancelamentoStatusException>();
		ex.Which.Message.Should().Contain("em andamento");
	}
}
