using CarWash.Application.Common.Exceptions;
using CarWash.Application.Filiais.ObterFilialPorId;
using CarWash.Application.Filiais.Persistence;
using CarWash.Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CarWash.UnitTests.Application.Filiais;

/// <summary>
/// RF018 — handler de leitura por id. Sem auditoria (leitura). 404 com mensagem
/// exata quando a filial não existe.
/// </summary>
public class ObterFilialPorIdHandlerTests
{
    private readonly IFilialRepository _repo = Substitute.For<IFilialRepository>();

    [Fact]
    public async Task Filial_existente_retorna_response()
    {
        var filial = Filial.Criar(Guid.NewGuid(), "Filial Centro", "FC01", 9, timezone: "America/Sao_Paulo");
        _repo.ObterPorIdAsync(filial.Id, Arg.Any<CancellationToken>()).Returns(filial);

        var handler = new ObterFilialPorIdHandler(_repo);
        var resposta = await handler.HandleAsync(new ObterFilialPorIdQuery(filial.Id), CancellationToken.None);

        resposta.Id.Should().Be(filial.Id);
        resposta.Nome.Should().Be("Filial Centro");
        resposta.CelulasAtivas.Should().Be(9);
        resposta.Ativa.Should().BeTrue();
    }

    [Fact]
    public async Task Filial_inexistente_lanca_NotFoundException_com_mensagem_exata()
    {
        _repo.ObterPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Filial?)null);

        var handler = new ObterFilialPorIdHandler(_repo);
        var act = () => handler.HandleAsync(new ObterFilialPorIdQuery(Guid.NewGuid()), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.Message.Should().Be(ObterFilialPorIdHandler.MensagemNaoEncontrado);
    }
}
