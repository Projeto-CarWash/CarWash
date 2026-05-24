using CarWash.Application.Servicos.Listar;
using CarWash.Application.Servicos.Persistence;
using CarWash.Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CarWash.UnitTests.Application.Servicos;

public class ListarServicosHandlerTests
{
    private readonly IServicoRepository _repo = Substitute.For<IServicoRepository>();

    [Fact]
    public async Task Retorna_pagina_efetiva_e_total_do_repositorio()
    {
        var lista = new List<Servico> { NovoServico() };
        _repo.ListarAsync("lav", true, 2, 50, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<Servico>)lista, 1));

        var handler = new ListarServicosHandler(_repo);
        var resposta = await handler.HandleAsync(
            new ListarServicosQuery("lav", true, 2, 50),
            CancellationToken.None);

        resposta.Total.Should().Be(1);
        resposta.Pagina.Should().Be(2);
        resposta.TamanhoPagina.Should().Be(50);
        resposta.Itens.Should().HaveCount(1);
        resposta.Itens[0].Nome.Should().Be("Lavagem Simples");
    }

    [Fact]
    public async Task Tamanho_pagina_fora_da_faixa_e_normalizado_na_resposta()
    {
        _repo.ListarAsync(null, null, 0, 999, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<Servico>)new List<Servico>(), 0));

        var handler = new ListarServicosHandler(_repo);
        var resposta = await handler.HandleAsync(
            new ListarServicosQuery(null, null, 0, 999),
            CancellationToken.None);

        resposta.Pagina.Should().Be(1);
        resposta.TamanhoPagina.Should().Be(100);
    }

    private static Servico NovoServico() => Servico.Criar(
        id: Guid.NewGuid(),
        nome: "Lavagem Simples",
        preco: 30m,
        duracaoMin: 30);
}
