using CarWash.Application.Agendamentos.Common;
using CarWash.Application.Agendamentos.ObterPorId;
using CarWash.Application.Agendamentos.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Domain.Entities;
using CarWash.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace CarWash.UnitTests.Application.Agendamentos;

public class ObterAgendamentoPorIdHandlerTests
{
    private static readonly Guid AgendamentoId = Guid.NewGuid();
    private static readonly Guid UsuarioId = Guid.NewGuid();

    private readonly IAgendamentoRepository _agendamentos = Substitute.For<IAgendamentoRepository>();
    private readonly IAgendamentoCatalogoRepository _catalogo = Substitute.For<IAgendamentoCatalogoRepository>();

    private ObterAgendamentoPorIdHandler NovoHandler() =>
        new(_agendamentos, _catalogo, NullLogger<ObterAgendamentoPorIdHandler>.Instance);

    private static ObterAgendamentoPorIdQuery NovaQuery() => new(AgendamentoId, "trace-1");

    private static Agendamento NovoAgendamento()
    {
        return Agendamento.Criar(
            id: AgendamentoId,
            filialId: Guid.NewGuid(),
            clienteId: Guid.NewGuid(),
            veiculoId: Guid.NewGuid(),
            criadoPor: UsuarioId,
            inicio: DateTime.UtcNow.AddHours(1),
            fim: DateTime.UtcNow.AddHours(2),
            responsavelId: Guid.NewGuid());
    }

    private static IReadOnlyCollection<AgendamentoItem> NovosItens(Guid servicoId)
    {
        return new[]
        {
            AgendamentoItem.Criar(
                id: Guid.NewGuid(),
                agendamentoId: AgendamentoId,
                servicoId: servicoId,
                precoAplicado: 50m,
                duracaoAplicada: 30),
        };
    }

    [Fact]
    public async Task Caminho_feliz_retorna_agendamento_com_dados_completos()
    {
        var agendamento = NovoAgendamento();
        var servicoId = Guid.NewGuid();
        var itens = NovosItens(servicoId);

        _agendamentos.ObterPorIdComItensAsync(AgendamentoId, Arg.Any<CancellationToken>())
            .Returns((agendamento, itens));

        _catalogo.ObterServicosAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new ServicoSnapshot(servicoId, "Lavagem Completa", 50m, 30, true),
            });

        var handler = NovoHandler();
        var resposta = await handler.HandleAsync(NovaQuery(), CancellationToken.None);

        resposta.Message.Should().Be("Agendamento encontrado.");
        resposta.Data.Id.Should().Be(AgendamentoId);
        resposta.Data.Status.Should().Be("agendado");
        resposta.Data.FilialId.Should().Be(agendamento.FilialId);
        resposta.Data.ClienteId.Should().Be(agendamento.ClienteId);
        resposta.Data.VeiculoId.Should().Be(agendamento.VeiculoId);
        resposta.Data.CriadoPor.Should().Be(agendamento.CriadoPor);
        resposta.Data.Itens.Should().HaveCount(1);
        resposta.Data.Itens[0].NomeServico.Should().Be("Lavagem Completa");
        resposta.TraceId.Should().Be("trace-1");
    }

    [Fact]
    public async Task Agendamento_nao_encontrado_lanca_NotFoundException()
    {
        _agendamentos.ObterPorIdComItensAsync(AgendamentoId, Arg.Any<CancellationToken>())
            .Returns(((Agendamento, IReadOnlyCollection<AgendamentoItem>)?)null);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovaQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Agendamento não encontrado*");
    }
}
