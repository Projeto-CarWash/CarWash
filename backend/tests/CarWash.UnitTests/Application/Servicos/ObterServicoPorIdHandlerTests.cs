using CarWash.Application.Common.Exceptions;
using CarWash.Application.Servicos.ObterPorId;
using CarWash.Application.Servicos.Persistence;
using CarWash.Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CarWash.UnitTests.Application.Servicos;

public class ObterServicoPorIdHandlerTests
{
    private readonly IServicoRepository _repo = Substitute.For<IServicoRepository>();

    [Fact]
    public async Task Servico_existente_retorna_response()
    {
        var servico = NovoServico();
        _repo.ObterPorIdAsync(servico.Id, Arg.Any<CancellationToken>()).Returns(servico);

        var handler = new ObterServicoPorIdHandler(_repo);
        var resposta = await handler.HandleAsync(new ObterServicoPorIdQuery(servico.Id), CancellationToken.None);

        resposta.Id.Should().Be(servico.Id);
        resposta.Nome.Should().Be("Lavagem Simples");
        resposta.Preco.Should().Be(30m);
        resposta.DuracaoMin.Should().Be(30);
    }

    [Fact]
    public async Task Servico_inexistente_lanca_NotFoundException()
    {
        _repo.ObterPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Servico?)null);

        var handler = new ObterServicoPorIdHandler(_repo);
        var act = () => handler.HandleAsync(new ObterServicoPorIdQuery(Guid.NewGuid()), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.Message.Should().Be(ObterServicoPorIdHandler.MensagemNaoEncontrado);
    }

    private static Servico NovoServico() => Servico.Criar(
        id: Guid.NewGuid(),
        nome: "Lavagem Simples",
        preco: 30m,
        duracaoMin: 30);
}
