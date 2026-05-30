using CarWash.Application.Clientes.Listar;
using CarWash.Application.Clientes.Persistence;
using CarWash.Domain.Entities;
using CarWash.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CarWash.UnitTests.Application.Clientes;

public class ListarClientesHandlerTests
{
    private readonly IClienteRepository _repo = Substitute.For<IClienteRepository>();

    [Fact]
    public async Task Retorna_pagina_efetiva_e_total_do_repositorio()
    {
        var lista = new List<Cliente> { NovoCliente() };
        _repo.ListarAsync("ana", true, 2, 50, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<Cliente>)lista, 1));

        var handler = new ListarClientesHandler(_repo);
        var resposta = await handler.HandleAsync(
            new ListarClientesQuery("ana", true, 2, 50),
            CancellationToken.None);

        resposta.Total.Should().Be(1);
        resposta.Pagina.Should().Be(2);
        resposta.TamanhoPagina.Should().Be(50);
        resposta.Itens.Should().HaveCount(1);
        resposta.Itens[0].Nome.Should().Be("Maria Souza");
    }

    [Fact]
    public async Task Tamanho_pagina_fora_da_faixa_e_normalizado_na_resposta()
    {
        _repo.ListarAsync(null, null, 0, 999, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<Cliente>)new List<Cliente>(), 0));

        var handler = new ListarClientesHandler(_repo);
        var resposta = await handler.HandleAsync(
            new ListarClientesQuery(null, null, 0, 999),
            CancellationToken.None);

        resposta.Pagina.Should().Be(1);
        resposta.TamanhoPagina.Should().Be(100);
    }

    private static Cliente NovoCliente() => Cliente.Criar(
        id: Guid.NewGuid(),
        nome: "Maria Souza",
        dataNascimento: new DateOnly(1990, 1, 1),
        celular: new Telefone("11987654321"),
        endereco: new Endereco("01001000", "Praça da Sé", "1", null, "Sé", "São Paulo", "SP"),
        cpf: new Cpf("39053344705"));
}
