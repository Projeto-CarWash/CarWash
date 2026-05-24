using CarWash.Application.Clientes.Atualizar;
using CarWash.Application.Clientes.Common;
using CarWash.Application.Clientes.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Domain.Entities;
using CarWash.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace CarWash.UnitTests.Application.Clientes;

public class AtualizarClienteHandlerTests
{
    private readonly IClienteRepository _repo = Substitute.For<IClienteRepository>();

    [Fact]
    public async Task Cliente_inexistente_lanca_NotFoundException()
    {
        _repo.ObterPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Cliente?)null);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Caminho_feliz_atualiza_dados_e_chama_salvar()
    {
        var cliente = NovoCliente();
        _repo.ObterPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);
        _repo.ExisteEmailAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);

        var handler = NovoHandler();
        var resposta = await handler.HandleAsync(NovoComando(cliente.Id), CancellationToken.None);

        resposta.Id.Should().Be(cliente.Id);
        resposta.Nome.Should().Be("Maria Souza Atualizada");
        await _repo.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Email_em_uso_por_outro_cliente_lanca_ConflictException()
    {
        var cliente = NovoCliente();
        _repo.ObterPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);
        _repo.ExisteEmailAsync("novo@x.com", cliente.Id, Arg.Any<CancellationToken>()).Returns(true);

        var handler = NovoHandler();
        var cmd = NovoComando(cliente.Id) with { Email = "novo@x.com" };
        var act = () => handler.HandleAsync(cmd, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.Which.Slug.Should().Be("cliente-email-duplicado");
    }

    private AtualizarClienteHandler NovoHandler() =>
        new(_repo, NullLogger<AtualizarClienteHandler>.Instance);

    private static Cliente NovoCliente() => Cliente.Criar(
        id: Guid.NewGuid(),
        nome: "Maria Souza",
        dataNascimento: new DateOnly(1990, 1, 1),
        celular: new Telefone("11987654321"),
        endereco: new Endereco("01001000", "Praça da Sé", "1", null, "Sé", "São Paulo", "SP"),
        cpf: new Cpf("39053344705"));

    private static AtualizarClienteCommand NovoComando(Guid id) => new(
        Id: id,
        Nome: "Maria Souza Atualizada",
        DataNascimento: new DateOnly(1990, 1, 1),
        Telefone: null,
        Celular: "11987654321",
        Email: null,
        Endereco: new EnderecoRequest
        {
            Cep = "01001000",
            Logradouro = "Praça da Sé",
            Numero = "1",
            Bairro = "Sé",
            Cidade = "São Paulo",
            Uf = "SP",
        },
        CamposExtras: null,
        UsuarioId: null);
}
