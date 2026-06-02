using CarWash.Application.Clientes.ObterPorId;
using CarWash.Application.Clientes.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Responsaveis.Persistence;
using CarWash.Domain.Entities;
using CarWash.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CarWash.UnitTests.Application.Clientes;

public class ObterClientePorIdHandlerTests
{
    private readonly IClienteRepository _repo = Substitute.For<IClienteRepository>();
    private readonly IResponsavelRepository _responsaveis = Substitute.For<IResponsavelRepository>();

    [Fact]
    public async Task Cliente_existente_retorna_response()
    {
        var cliente = NovoCliente();
        _repo.ObterPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);
        _responsaveis.ListarPorClienteTitularIdAsync(cliente.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Responsavel>());

        var handler = new ObterClientePorIdHandler(_repo, _responsaveis);
        var resposta = await handler.HandleAsync(new ObterClientePorIdQuery(cliente.Id), CancellationToken.None);

        resposta.Id.Should().Be(cliente.Id);
        resposta.Nome.Should().Be("Maria Souza");
        resposta.Endereco.Cidade.Should().Be("São Paulo");
        resposta.Responsaveis.Should().BeEmpty();
    }

    [Fact]
    public async Task Cliente_inexistente_lanca_NotFoundException()
    {
        _repo.ObterPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Cliente?)null);

        var handler = new ObterClientePorIdHandler(_repo, _responsaveis);
        var act = () => handler.HandleAsync(new ObterClientePorIdQuery(Guid.NewGuid()), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.Message.Should().Be(ObterClientePorIdHandler.MensagemNaoEncontrado);
    }

    private static Cliente NovoCliente() => Cliente.Criar(
        id: Guid.NewGuid(),
        nome: "Maria Souza",
        dataNascimento: new DateOnly(1990, 1, 1),
        celular: new Telefone("11987654321"),
        endereco: new Endereco("01001000", "Praça da Sé", "1", null, "Sé", "São Paulo", "SP"),
        cpf: new Cpf("39053344705"));
}
