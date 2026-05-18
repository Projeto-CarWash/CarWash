using CarWash.Application.Clientes.AlterarStatus;
using CarWash.Application.Clientes.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Domain.Entities;
using CarWash.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CarWash.UnitTests.Application.Clientes;

public class AlterarStatusClienteHandlerTests
{
    private readonly IClienteRepository _repo = Substitute.For<IClienteRepository>();

    [Fact]
    public async Task Inativar_cliente_ativo_salva_e_retorna_estado()
    {
        var cliente = NovoCliente();
        _repo.ObterPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);

        var handler = new AlterarStatusClienteHandler(_repo);
        var resposta = await handler.HandleAsync(
            new AlterarStatusClienteCommand(cliente.Id, false, UsuarioId: null),
            CancellationToken.None);

        resposta.Ativo.Should().BeFalse();
        cliente.Ativo.Should().BeFalse();
        await _repo.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cliente_inexistente_lanca_NotFoundException()
    {
        _repo.ObterPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Cliente?)null);

        var handler = new AlterarStatusClienteHandler(_repo);
        var act = () => handler.HandleAsync(
            new AlterarStatusClienteCommand(Guid.NewGuid(), true, UsuarioId: null),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _repo.DidNotReceive().SalvarAsync(Arg.Any<CancellationToken>());
    }

    private static Cliente NovoCliente() => Cliente.Criar(
        id: Guid.NewGuid(),
        nome: "Maria Souza",
        dataNascimento: new DateOnly(1990, 1, 1),
        celular: new Telefone("11987654321"),
        endereco: new Endereco("01001000", "Praça da Sé", "1", null, "Sé", "São Paulo", "SP"),
        cpf: new Cpf("39053344705"));
}
