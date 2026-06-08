using CarWash.Application.Agendamentos.ObterPorId;
using CarWash.Application.Agendamentos.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Domain.Entities;
using CarWash.Domain.Enums;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CarWash.UnitTests.Application.Agendamentos.ObterPorId;

public class ObterAgendamentoPorIdHandlerTests
{
    private readonly IAgendamentoRepository _repo = Substitute.For<IAgendamentoRepository>();

    [Fact]
    public async Task Agendamento_encontrado_retorna_response_com_dados()
    {
        var agendamento = AgendamentoAtivo();
        _repo.ObterPorIdAsync(agendamento.Id, Arg.Any<CancellationToken>()).Returns(agendamento);

        var handler = NovoHandler();
        var resposta = await handler.HandleAsync(
            new ObterAgendamentoPorIdQuery(agendamento.Id), CancellationToken.None);

        resposta.Message.Should().Be("Agendamento encontrado.");
        resposta.Data.Id.Should().Be(agendamento.Id);
        resposta.Data.Status.Should().Be("AGENDADO");
    }

    [Fact]
    public async Task Agendamento_nao_encontrado_lanca_NotFoundException()
    {
        var id = Guid.NewGuid();
        _repo.ObterPorIdAsync(id, Arg.Any<CancellationToken>()).Returns((Agendamento?)null);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(new ObterAgendamentoPorIdQuery(id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Agendamento*");
    }

    [Fact]
    public async Task Query_null_lanca_ArgumentNullException()
    {
        var handler = NovoHandler();
        var act = () => handler.HandleAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    private ObterAgendamentoPorIdHandler NovoHandler() => new(_repo);

    private Agendamento AgendamentoAtivo()
    {
        var filialId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var clienteId = Guid.Parse("20000000-0000-0000-0000-000000000002");
        var veiculoId = Guid.Parse("30000000-0000-0000-0000-000000000003");
        var criadoPor = Guid.Parse("50000000-0000-0000-0000-000000000005");
        var inicio = DateTime.UtcNow.AddHours(1);
        var fim = inicio.AddMinutes(30);

        return Agendamento.Criar(
            id: Guid.NewGuid(),
            filialId: filialId,
            clienteId: clienteId,
            veiculoId: veiculoId,
            criadoPor: criadoPor,
            inicio: inicio,
            fim: fim,
            duracaoTotalMin: 30,
            valorTotal: 50m);
    }
}
