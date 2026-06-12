using CarWash.Application.Common.Exceptions;
using CarWash.Application.Responsaveis.AlterarStatus;
using CarWash.Application.Responsaveis.Persistence;
using CarWash.Domain.Entities;
using CarWash.Domain.Enums;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CarWash.UnitTests.Application.Responsaveis;

public class AlterarStatusResponsavelHandlerTests
{
    private readonly IResponsavelRepository _repositorio = Substitute.For<IResponsavelRepository>();

    [Fact]
    public async Task Inativar_responsavel_ativo_persiste_e_retorna_ativo_false()
    {
        var responsavel = NovoResponsavel();
        _repositorio.ObterPorIdRastreadoAsync(responsavel.Id, responsavel.ClienteTitularId, Arg.Any<CancellationToken>())
            .Returns(responsavel);

        var handler = new AlterarStatusResponsavelHandler(_repositorio);
        var resposta = await handler.HandleAsync(NovoComando(responsavel, ativo: false), CancellationToken.None);

        resposta.Ativo.Should().BeFalse();
        responsavel.Ativo.Should().BeFalse();
        await _repositorio.Received(1).SalvarAsync("trace-1", Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reativar_responsavel_inativo_retorna_ativo_true()
    {
        var responsavel = NovoResponsavel();
        responsavel.Inativar();
        _repositorio.ObterPorIdRastreadoAsync(responsavel.Id, responsavel.ClienteTitularId, Arg.Any<CancellationToken>())
            .Returns(responsavel);

        var handler = new AlterarStatusResponsavelHandler(_repositorio);
        var resposta = await handler.HandleAsync(NovoComando(responsavel, ativo: true), CancellationToken.None);

        resposta.Ativo.Should().BeTrue();
        responsavel.Ativo.Should().BeTrue();
    }

    [Fact]
    public async Task Responsavel_inexistente_lanca_NotFoundException()
    {
        _repositorio.ObterPorIdRastreadoAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Responsavel?)null);

        var handler = new AlterarStatusResponsavelHandler(_repositorio);
        var act = () => handler.HandleAsync(NovoComando(NovoResponsavel(), ativo: false), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _repositorio.DidNotReceive().SalvarAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    private static AlterarStatusResponsavelCommand NovoComando(Responsavel responsavel, bool ativo) => new(
        ResponsavelId: responsavel.Id,
        ClienteTitularId: responsavel.ClienteTitularId,
        Ativo: ativo,
        TraceId: "trace-1",
        UsuarioId: Guid.NewGuid());

    private static Responsavel NovoResponsavel() => Responsavel.Criar(
        id: Guid.NewGuid(),
        clienteTitularId: Guid.NewGuid(),
        nome: "João Original",
        documento: "39053344705",
        grauVinculo: GrauVinculo.ResponsavelFinanceiro);
}
