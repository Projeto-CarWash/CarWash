using CarWash.Application.Clientes.ObterPorId;
using CarWash.Application.Clientes.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Interfaces;
using CarWash.Application.Responsaveis.Persistence;
using CarWash.Domain.Entities;
using CarWash.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CarWash.UnitTests.Application.Clientes;

public class ObterClientePorIdHandlerTests
{
    private readonly IClienteRepository _repo = Substitute.For<IClienteRepository>();
    private readonly IResponsavelRepository _responsaveis = Substitute.For<IResponsavelRepository>();
    private readonly ICarWashDbContext _context = Substitute.For<ICarWashDbContext>();

    [Fact]
    public async Task Cliente_existente_retorna_response()
    {
        var cliente = NovoCliente();
        _repo.ObterPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);
        _responsaveis.ListarPorClienteTitularIdAsync(cliente.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Responsavel>());

        var veiculosList = new List<Veiculo>();
        var dbSet = TestDbSetHelper.CreateMockDbSet(veiculosList);
        _context.Veiculos.Returns(dbSet);

        var handler = new ObterClientePorIdHandler(_repo, _responsaveis, _context);
        var resposta = await handler.HandleAsync(new ObterClientePorIdQuery(cliente.Id), CancellationToken.None);

        resposta.Id.Should().Be(cliente.Id);
        resposta.Nome.Should().Be("Maria Souza");
        resposta.Endereco.Cidade.Should().Be("São Paulo");
        resposta.Responsaveis.Should().BeEmpty();
        resposta.Veiculos.Should().BeEmpty();
    }

    [Fact]
    public async Task Cliente_existente_com_veiculos_retorna_response_com_veiculos()
    {
        var cliente = NovoCliente();
        _repo.ObterPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);
        _responsaveis.ListarPorClienteTitularIdAsync(cliente.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Responsavel>());

        var veiculo = Veiculo.Criar(
            Guid.NewGuid(),
            cliente.Id,
            new Placa("ABC1D23"),
            "Civic",
            "Honda",
            "Preto"
        );

        var veiculosList = new List<Veiculo> { veiculo };
        var dbSet = TestDbSetHelper.CreateMockDbSet(veiculosList);
        _context.Veiculos.Returns(dbSet);

        var handler = new ObterClientePorIdHandler(_repo, _responsaveis, _context);
        var resposta = await handler.HandleAsync(new ObterClientePorIdQuery(cliente.Id), CancellationToken.None);

        resposta.Id.Should().Be(cliente.Id);
        resposta.Veiculos.Should().HaveCount(1);
        resposta.Veiculos[0].Placa.Should().Be("ABC1D23");
        resposta.Veiculos[0].Modelo.Should().Be("Civic");
    }

    [Fact]
    public async Task Cliente_inexistente_lanca_NotFoundException()
    {
        _repo.ObterPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Cliente?)null);

        var veiculosList = new List<Veiculo>();
        var dbSet = TestDbSetHelper.CreateMockDbSet(veiculosList);
        _context.Veiculos.Returns(dbSet);

        var handler = new ObterClientePorIdHandler(_repo, _responsaveis, _context);
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
