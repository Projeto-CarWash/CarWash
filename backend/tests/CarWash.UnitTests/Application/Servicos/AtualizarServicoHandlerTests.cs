using CarWash.Application.Common.Exceptions;
using CarWash.Application.Servicos.Common;
using CarWash.Application.Servicos.Atualizar;
using CarWash.Application.Servicos.Persistence;
using CarWash.Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CarWash.UnitTests.Application.Servicos;

public class AtualizarServicoHandlerTests
{
    private readonly IServicoRepository _repo = Substitute.For<IServicoRepository>();

    [Fact]
    public async Task Caminho_feliz_atualiza_dados_e_retorna_response()
    {
        var servico = NovoServico();
        _repo.ObterPorIdAsync(servico.Id, Arg.Any<CancellationToken>()).Returns(servico);
        _repo.ExisteNomeAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);

        var handler = NovoHandler();
        var resposta = await handler.HandleAsync(
            new AtualizarServicoCommand(servico.Id, "Lavagem Premium", 50m, 45, "trace-1", UsuarioId: null),
            CancellationToken.None);

        resposta.Nome.Should().Be("Lavagem Premium");
        resposta.Preco.Should().Be(50m);
        resposta.DuracaoMin.Should().Be(45);
        await _repo.Received(1).RegistrarAuditoriaAsync(
            "SERVICO_ATUALIZADO", servico.Id, "trace-1", null, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Servico_inexistente_lanca_NotFoundException()
    {
        _repo.ObterPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Servico?)null);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(
            new AtualizarServicoCommand(Guid.NewGuid(), "Nome", 10m, 30, "trace-1", UsuarioId: null),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Nome_duplicado_lanca_ConflictException()
    {
        var servico = NovoServico();
        _repo.ObterPorIdAsync(servico.Id, Arg.Any<CancellationToken>()).Returns(servico);
        _repo.ExisteNomeAsync("Nome Duplicado", servico.Id, Arg.Any<CancellationToken>()).Returns(true);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(
            new AtualizarServicoCommand(servico.Id, "Nome Duplicado", 30m, 30, "trace-1", UsuarioId: null),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.Which.Slug.Should().Be("servico-nome-duplicado");
    }

    private AtualizarServicoHandler NovoHandler() => new(_repo);

    private static Servico NovoServico() => Servico.Criar(
        id: Guid.NewGuid(),
        nome: "Lavagem Simples",
        preco: 30m,
        duracaoMin: 30);
}
