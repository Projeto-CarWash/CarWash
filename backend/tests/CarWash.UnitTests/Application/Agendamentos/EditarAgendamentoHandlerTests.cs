using CarWash.Application.Agendamentos.Common;
using CarWash.Application.Agendamentos.Editar;
using CarWash.Application.Agendamentos.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Domain.Common;
using CarWash.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ValidationException = CarWash.Application.Common.Exceptions.ValidationException;
using Xunit;

namespace CarWash.UnitTests.Application.Agendamentos;

public class EditarAgendamentoHandlerTests
{
    private static readonly Guid AgendamentoId = Guid.NewGuid();
    private static readonly Guid UsuarioId = Guid.NewGuid();

    private readonly IAgendamentoRepository _agendamentos = Substitute.For<IAgendamentoRepository>();

    private EditarAgendamentoHandler NovoHandler() =>
        new(_agendamentos, NullLogger<EditarAgendamentoHandler>.Instance);

    private EditarAgendamentoCommand NovoComando(DateTime? inicio = null, DateTime? fim = null) =>
        new(AgendamentoId, inicio, fim, null, null, "trace-1", UsuarioId);

    private Agendamento NovoAgendamento()
    {
        return Agendamento.Criar(
            id: AgendamentoId,
            filialId: Guid.NewGuid(),
            clienteId: Guid.NewGuid(),
            veiculoId: Guid.NewGuid(),
            criadoPor: UsuarioId,
            inicio: DateTime.UtcNow.AddHours(1),
            fim: DateTime.UtcNow.AddHours(2));
    }

    [Fact]
    public async Task Caminho_feliz_edita_agendamento_agendado()
    {
        var agendamento = NovoAgendamento();
        _agendamentos.ObterPorIdRastreadoAsync(AgendamentoId, Arg.Any<CancellationToken>())
            .Returns(agendamento);

        var novoInicio = DateTime.UtcNow.AddHours(3);
        var novoFim = DateTime.UtcNow.AddHours(4);
        var handler = NovoHandler();
        var command = NovoComando(novoInicio, novoFim);

        var resposta = await handler.HandleAsync(command, CancellationToken.None);

        resposta.Message.Should().Be("Agendamento atualizado com sucesso.");
        resposta.Data.Id.Should().Be(AgendamentoId);
        resposta.Data.Status.Should().Be("agendado");
        resposta.Data.AtualizadoEm.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        await _agendamentos.Received(1).SalvarAsync(
            Arg.Is(agendamento),
            Arg.Is<AgendamentoHistorico>(h => h.AgendamentoId == AgendamentoId),
            "trace-1",
            "AGENDAMENTO_EDITADO",
            UsuarioId,
            Arg.Any<string>(),
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
        var command = new EditarAgendamentoCommand(
            AgendamentoId, null, null, null, null, "trace-1", null);

        var act = () => handler.HandleAsync(command, CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Agendamento_finalizado_lanca_EdicaoBloqueadaException()
    {
        var agendamento = NovoAgendamento();
        agendamento.Iniciar();
        agendamento.Finalizar();
        _agendamentos.ObterPorIdRastreadoAsync(AgendamentoId, Arg.Any<CancellationToken>())
            .Returns(agendamento);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<EdicaoBloqueadaException>();
        ex.Which.Message.Should().Contain("finalizado");
    }

    [Fact]
    public async Task Agendamento_cancelado_lanca_EdicaoBloqueadaException()
    {
        var agendamento = NovoAgendamento();
        agendamento.Cancelar("Cancelamento anterior", UsuarioId);
        _agendamentos.ObterPorIdRastreadoAsync(AgendamentoId, Arg.Any<CancellationToken>())
            .Returns(agendamento);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<EdicaoBloqueadaException>();
        ex.Which.Message.Should().Contain("cancelado");
    }

    [Fact]
    public async Task Agendamento_em_andamento_lanca_EdicaoBloqueadaException()
    {
        var agendamento = NovoAgendamento();
        agendamento.Iniciar();
        _agendamentos.ObterPorIdRastreadoAsync(AgendamentoId, Arg.Any<CancellationToken>())
            .Returns(agendamento);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<EdicaoBloqueadaException>();
        ex.Which.Message.Should().Contain("não permite edição");
    }
}
