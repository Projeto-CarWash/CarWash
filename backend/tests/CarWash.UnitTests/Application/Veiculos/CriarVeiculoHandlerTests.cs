using CarWash.Application.Clientes.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Veiculos.Common;
using CarWash.Application.Veiculos.Criar;
using CarWash.Application.Veiculos.Persistence;
using CarWash.Domain.Common;
using CarWash.Domain.Entities;
using CarWash.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CarWash.UnitTests.Application.Veiculos;

public class CriarVeiculoHandlerTests
{
    private readonly IClienteRepository _clientes = Substitute.For<IClienteRepository>();
    private readonly IVeiculoRepository _veiculos = Substitute.For<IVeiculoRepository>();

    [Fact]
    public async Task Caminho_feliz_persiste_veiculo_e_retorna_response()
    {
        var cliente = NovoCliente();
        _clientes.ObterPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);
        _veiculos.ExistePlacaAsync("ABC1D23", Arg.Any<CancellationToken>()).Returns(false);

        var handler = NovoHandler();
        var resposta = await handler.HandleAsync(NovoComando(cliente.Id), CancellationToken.None);

        resposta.Id.Should().NotBeEmpty();
        resposta.ClienteId.Should().Be(cliente.Id);
        resposta.Placa.Should().Be("ABC1D23");
        resposta.Modelo.Should().Be("Onix");
        resposta.Ativo.Should().BeTrue();

        await _veiculos.Received(1).AdicionarAsync(
            Arg.Is<Veiculo>(v => v.Placa == "ABC1D23" && v.ClienteId == cliente.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cliente_inexistente_lanca_NotFoundException()
    {
        _clientes.ObterPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Cliente?)null);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Placa_invalida_propaga_DomainException()
    {
        var cliente = NovoCliente();
        _clientes.ObterPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(cliente.Id) with { Placa = "XX-INVALIDA" }, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Placa_duplicada_lanca_PlacaJaCadastradaException_com_slug()
    {
        var cliente = NovoCliente();
        _clientes.ObterPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);
        _veiculos.ExistePlacaAsync("ABC1D23", Arg.Any<CancellationToken>()).Returns(true);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(cliente.Id), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<PlacaJaCadastradaException>();
        ex.Which.Slug.Should().Be(PlacaJaCadastradaException.SlugPadrao);
    }

    [Fact]
    public async Task Cliente_inativo_lanca_RecursoInativoException()
    {
        var cliente = NovoCliente();
        cliente.Inativar();
        _clientes.ObterPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);
        _veiculos.ExistePlacaAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(cliente.Id), CancellationToken.None);

        await act.Should().ThrowAsync<RecursoInativoException>();
    }

    [Fact]
    public async Task Placa_eh_normalizada_em_uppercase_sem_espacos_antes_da_persistencia()
    {
        var cliente = NovoCliente();
        _clientes.ObterPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);
        _veiculos.ExistePlacaAsync("ABC1D23", Arg.Any<CancellationToken>()).Returns(false);

        var handler = NovoHandler();
        var resposta = await handler.HandleAsync(NovoComando(cliente.Id) with { Placa = " abc-1d23 " }, CancellationToken.None);

        resposta.Placa.Should().Be("ABC1D23");
    }

    private CriarVeiculoHandler NovoHandler() => new(_clientes, _veiculos);

    private static CriarVeiculoCommand NovoComando(Guid clienteId) => new(
        ClienteId: clienteId,
        Placa: "ABC1D23",
        Modelo: "Onix",
        Fabricante: "Chevrolet",
        Cor: "Prata",
        Ano: 2022,
        TraceId: "trace-1",
        UsuarioId: null);

    private static Cliente NovoCliente() => Cliente.Criar(
        id: Guid.NewGuid(),
        nome: "Maria Souza",
        dataNascimento: new DateOnly(1990, 1, 1),
        celular: new Telefone("11987654321"),
        endereco: new Endereco(
            cep: "01001000",
            logradouro: "Praça da Sé",
            numero: "1",
            complemento: null,
            bairro: "Sé",
            cidade: "São Paulo",
            uf: "SP"),
        cpf: new Cpf("39053344705"));
}
