using CarWash.Application.Common.Exceptions;
using CarWash.Application.Servicos.AlterarStatus;
using CarWash.Application.Servicos.Persistence;
using CarWash.Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CarWash.UnitTests.Application.Servicos;

public class AlterarStatusServicoHandlerTests
{
    private readonly IServicoRepository _repo = Substitute.For<IServicoRepository>();

    [Fact]
    public async Task Inativar_servico_ativo_salva_e_retorna_estado()
    {
        var servico = NovoServico();
        _repo.ObterPorIdAsync(servico.Id, Arg.Any<CancellationToken>()).Returns(servico);

        var handler = new AlterarStatusServicoHandler(_repo);
        var resposta = await handler.HandleAsync(
            new AlterarStatusServicoCommand(servico.Id, false, "trace-1", UsuarioId: null),
            CancellationToken.None);

        resposta.Ativo.Should().BeFalse();
        servico.Ativo.Should().BeFalse();
        await _repo.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
        await _repo.Received(1).RegistrarAuditoriaAsync(
            "SERVICO_DESATIVADO", servico.Id, "trace-1", null, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Ativar_servico_inativo_salva_e_retorna_estado()
    {
        var servico = NovoServico();
        servico.Inativar();
        _repo.ObterPorIdAsync(servico.Id, Arg.Any<CancellationToken>()).Returns(servico);

        var handler = new AlterarStatusServicoHandler(_repo);
        var resposta = await handler.HandleAsync(
            new AlterarStatusServicoCommand(servico.Id, true, "trace-2", UsuarioId: null),
            CancellationToken.None);

        resposta.Ativo.Should().BeTrue();
        servico.Ativo.Should().BeTrue();
        await _repo.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
        await _repo.Received(1).RegistrarAuditoriaAsync(
            "SERVICO_ATIVADO", servico.Id, "trace-2", null, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Servico_inexistente_lanca_NotFoundException()
    {
        _repo.ObterPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Servico?)null);

        var handler = new AlterarStatusServicoHandler(_repo);
        var act = () => handler.HandleAsync(
            new AlterarStatusServicoCommand(Guid.NewGuid(), true, "trace-3", UsuarioId: null),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _repo.DidNotReceive().SalvarAsync(Arg.Any<CancellationToken>());
    }

    private static Servico NovoServico() => Servico.Criar(
        id: Guid.NewGuid(),
        nome: "Lavagem Simples",
        preco: 30m,
        duracaoMin: 30);
}
