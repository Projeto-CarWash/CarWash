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
    public async Task Placa_com_caractere_especial_lanca_DomainException()
    {
        var cliente = NovoCliente();
        _clientes.ObterPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(cliente.Id) with { Placa = "ABC-123" }, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("A placa informada não está em um formato válido.");
    }

    [Fact]
    public async Task Placa_menos_de_7_caracteres_lanca_DomainException()
    {
        var cliente = NovoCliente();
        _clientes.ObterPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(cliente.Id) with { Placa = "ABC12" }, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("A placa informada não está em um formato válido.");
    }

    [Fact]
    public async Task Placa_mais_de_7_caracteres_lanca_DomainException()
    {
        var cliente = NovoCliente();
        _clientes.ObterPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(cliente.Id) with { Placa = "ABC12345" }, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("A placa informada não está em um formato válido.");
    }

    [Fact]
    public async Task Placa_somente_numeros_lanca_DomainException()
    {
        var cliente = NovoCliente();
        _clientes.ObterPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(cliente.Id) with { Placa = "1234567" }, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("A placa informada não está em um formato válido.");
    }

    [Fact]
    public async Task Placa_somente_letras_lanca_DomainException()
    {
        var cliente = NovoCliente();
        _clientes.ObterPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(cliente.Id) with { Placa = "ABCDEFG" }, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("A placa informada não está em um formato válido.");
    }

    [Fact]
    public async Task Placa_nula_lanca_DomainException()
    {
        var cliente = NovoCliente();
        _clientes.ObterPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(cliente.Id) with { Placa = null }, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("O campo placa é obrigatório.");
    }

    [Fact]
    public async Task Placa_vazia_lanca_DomainException()
    {
        var cliente = NovoCliente();
        _clientes.ObterPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(cliente.Id) with { Placa = "" }, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("O campo placa é obrigatório.");
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
        ex.Which.Message.Should().Be("Já existe um veículo cadastrado com a placa informada.");
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
    public async Task Placa_eh_normalizada_em_uppercase_com_trim_antes_da_persistencia()
    {
        var cliente = NovoCliente();
        _clientes.ObterPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);
        _veiculos.ExistePlacaAsync("ABC1D23", Arg.Any<CancellationToken>()).Returns(false);

        var handler = NovoHandler();
        var resposta = await handler.HandleAsync(NovoComando(cliente.Id) with { Placa = " abc1d23 " }, CancellationToken.None);

        resposta.Placa.Should().Be("ABC1D23");
    }

    [Fact]
    public async Task Placa_padrao_antigo_eh_aceita()
    {
        var cliente = NovoCliente();
        _clientes.ObterPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);
        _veiculos.ExistePlacaAsync("ABC1234", Arg.Any<CancellationToken>()).Returns(false);

        var handler = NovoHandler();
        var resposta = await handler.HandleAsync(NovoComando(cliente.Id) with { Placa = "ABC1234" }, CancellationToken.None);

        resposta.Placa.Should().Be("ABC1234");
    }

    [Fact]
    public async Task Placa_padrao_mercosul_eh_aceita()
    {
        var cliente = NovoCliente();
        _clientes.ObterPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);
        _veiculos.ExistePlacaAsync("ABC1D23", Arg.Any<CancellationToken>()).Returns(false);

        var handler = NovoHandler();
        var resposta = await handler.HandleAsync(NovoComando(cliente.Id) with { Placa = "ABC1D23" }, CancellationToken.None);

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
