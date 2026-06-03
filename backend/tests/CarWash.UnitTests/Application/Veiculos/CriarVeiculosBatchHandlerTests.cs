using CarWash.Application.Clientes.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Veiculos.Common;
using CarWash.Application.Veiculos.CriarBatch;
using CarWash.Application.Veiculos.Persistence;
using CarWash.Domain.Common;
using CarWash.Domain.Entities;
using CarWash.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CarWash.UnitTests.Application.Veiculos;

public class CriarVeiculosBatchHandlerTests
{
    private static readonly string[] PlacasExistentes = { "ABC1D23" };

    private readonly IClienteRepository _clientes = Substitute.For<IClienteRepository>();
    private readonly IVeiculoRepository _veiculos = Substitute.For<IVeiculoRepository>();

    [Fact]
    public async Task Batch_feliz_persiste_todos_e_retorna_responses()
    {
        var cliente = NovoCliente();
        _clientes.ObterPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);
        _veiculos.PlacasExistentesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<string>());

        var handler = NovoHandler();
        var command = NovoBatchComando(cliente.Id, [
            new VeiculoItemCommand("ABC1D23", "Onix", "Chevrolet", "Prata", 2022),
            new VeiculoItemCommand("XYZ1234", "Corolla", "Toyota", "Preto", 2023),
        ]);

        var resposta = await handler.HandleAsync(command, CancellationToken.None);

        resposta.Should().HaveCount(2);
        resposta[0].Placa.Should().Be("ABC1D23");
        resposta[1].Placa.Should().Be("XYZ1234");

        await _veiculos.Received(1).AdicionarRangeAsync(
            Arg.Is<IEnumerable<Veiculo>>(v => v.Count() == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Batch_com_placa_duplicada_no_payload_lanca_PlacaDuplicadaPayloadException()
    {
        var cliente = NovoCliente();
        _clientes.ObterPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);

        var handler = NovoHandler();
        var command = NovoBatchComando(cliente.Id, [
            new VeiculoItemCommand("ABC1D23", "Onix", "Chevrolet", "Prata", 2022),
            new VeiculoItemCommand("ABC1D23", "Corolla", "Toyota", "Preto", 2023),
        ]);

        var act = () => handler.HandleAsync(command, CancellationToken.None);

        await act.Should().ThrowAsync<PlacaDuplicadaPayloadException>()
            .WithMessage("O payload contém placas duplicadas.");
    }

    [Fact]
    public async Task Batch_com_placa_ja_existente_no_banco_lanca_PlacaJaCadastradaException()
    {
        var cliente = NovoCliente();
        _clientes.ObterPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);
        _veiculos.PlacasExistentesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(PlacasExistentes);

        var handler = NovoHandler();
        var command = NovoBatchComando(cliente.Id, [
            new VeiculoItemCommand("ABC1D23", "Onix", "Chevrolet", "Prata", 2022),
        ]);

        var act = () => handler.HandleAsync(command, CancellationToken.None);

        await act.Should().ThrowAsync<PlacaJaCadastradaException>();
    }

    [Fact]
    public async Task Batch_com_veiculo_invalido_nao_persiste_nenhum()
    {
        var cliente = NovoCliente();
        _clientes.ObterPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);
        _veiculos.PlacasExistentesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<string>());

        var handler = NovoHandler();
        var command = NovoBatchComando(cliente.Id, [
            new VeiculoItemCommand("ABC1D23", "Onix", "Chevrolet", "Prata", 2022),
            new VeiculoItemCommand("INVALIDA", "Corolla", "Toyota", "Preto", 2023),
        ]);

        var act = () => handler.HandleAsync(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();

        await _veiculos.DidNotReceive().AdicionarRangeAsync(
            Arg.Any<IEnumerable<Veiculo>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Batch_cliente_inativo_lanca_RecursoInativoException()
    {
        var cliente = NovoCliente();
        cliente.Inativar();
        _clientes.ObterPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);
        _veiculos.PlacasExistentesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<string>());

        var handler = NovoHandler();
        var command = NovoBatchComando(cliente.Id, [
            new VeiculoItemCommand("ABC1D23", "Onix", "Chevrolet", "Prata", 2022),
        ]);

        var act = () => handler.HandleAsync(command, CancellationToken.None);

        await act.Should().ThrowAsync<RecursoInativoException>();
    }

    private CriarVeiculosBatchHandler NovoHandler() => new(_clientes, _veiculos);

    private static CriarVeiculosBatchCommand NovoBatchComando(Guid clienteId, List<VeiculoItemCommand> itens) => new(
        ClienteId: clienteId,
        Veiculos: itens,
        TraceId: "trace-batch-1",
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
