using CarWash.Application.Common.Exceptions;
using CarWash.Application.Servicos.Common;
using CarWash.Application.Servicos.Criar;
using CarWash.Application.Servicos.Persistence;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CarWash.UnitTests.Application.Servicos;

public class CriarServicoHandlerTests
{
    private readonly IServicoRepository _repo = Substitute.For<IServicoRepository>();

    [Fact]
    public async Task Caminho_feliz_chama_repo_e_retorna_response_com_traceId()
    {
        _repo.ExisteNomeAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);

        var handler = NovoHandler();
        var resposta = await handler.HandleAsync(NovoComando(), CancellationToken.None);

        resposta.Id.Should().NotBeEmpty();
        resposta.TraceId.Should().Be("trace-1");
        resposta.Mensagem.Should().Be("Serviço cadastrado com sucesso.");
    }

    [Fact]
    public async Task Nome_duplicado_lanca_ConflictException()
    {
        _repo.ExisteNomeAsync("Lavagem Simples", Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando() with { Nome = "Lavagem Simples" }, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.Which.Slug.Should().Be("servico-nome-duplicado");
    }

    [Fact]
    public async Task Preco_null_dispara_ValidationException()
    {
        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando() with { Preco = null }, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task DuracaoMin_null_dispara_ValidationException()
    {
        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando() with { DuracaoMin = null }, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    private CriarServicoHandler NovoHandler() => new(_repo);

    private static CriarServicoCommand NovoComando() => new(
        Nome: "Lavagem Simples",
        Preco: 30m,
        DuracaoMin: 30,
        TraceId: "trace-1",
        UsuarioId: null);
}
