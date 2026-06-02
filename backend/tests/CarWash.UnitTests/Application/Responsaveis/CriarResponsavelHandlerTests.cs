using CarWash.Application.Clientes.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Responsaveis.Criar;
using CarWash.Application.Responsaveis.Persistence;
using CarWash.Domain.Entities;
using CarWash.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CarWash.UnitTests.Application.Responsaveis;

public class CriarResponsavelHandlerTests
{
    private readonly IClienteRepository _clientes = Substitute.For<IClienteRepository>();
    private readonly IResponsavelRepository _responsaveis = Substitute.For<IResponsavelRepository>();

    [Fact]
    public async Task Caminho_feliz_chama_repo_e_retorna_response_com_traceId()
    {
        var cliente = NovoCliente();
        _clientes.ObterPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);
        _responsaveis.ExisteDocumentoAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var handler = NovoHandler();
        var comando = NovoComando(cliente.Id);
        var resposta = await handler.HandleAsync(comando, CancellationToken.None);

        resposta.Id.Should().NotBeEmpty();
        resposta.ClienteTitularId.Should().Be(cliente.Id);
        resposta.Nome.Should().Be("João Silva");
        resposta.Documento.Should().Be("39053344705");
        resposta.GrauVinculo.Should().Be("RESPONSAVEL_FINANCEIRO");
        resposta.Mensagem.Should().Be("Responsável cadastrado com sucesso.");
        resposta.TraceId.Should().Be("trace-1");

        await _responsaveis.Received(1).AdicionarAsync(
            Arg.Is<Responsavel>(r => r.Nome == "João Silva" && r.Documento == "39053344705"),
            "trace-1",
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cliente_titular_nao_encontrado_lanca_NotFoundException()
    {
        _clientes.ObterPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Cliente?)null);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(Guid.NewGuid()), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.Message.Should().Be("Cliente titular não encontrado.");
    }

    [Fact]
    public async Task Documento_duplicado_lanca_ConflictException()
    {
        var cliente = NovoCliente();
        _clientes.ObterPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);
        _responsaveis.ExisteDocumentoAsync("39053344705", Arg.Any<CancellationToken>()).Returns(true);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(cliente.Id), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.Which.Slug.Should().Be("responsavel-documento-duplicado");
    }

    private CriarResponsavelHandler NovoHandler() => new(_clientes, _responsaveis);

    private static CriarResponsavelCommand NovoComando(Guid clienteTitularId) => new(
        ClienteTitularId: clienteTitularId,
        Nome: "João Silva",
        Documento: "39053344705",
        Telefone: "11987654321",
        Email: "joao@email.com",
        GrauVinculo: "RESPONSAVEL_FINANCEIRO",
        TraceId: "trace-1",
        UsuarioId: null);

    private static Cliente NovoCliente() => Cliente.Criar(
        id: Guid.NewGuid(),
        nome: "Maria Souza",
        dataNascimento: new DateOnly(1990, 1, 1),
        celular: new Telefone("11987654321"),
        endereco: new Endereco("01001000", "Praça da Sé", "1", null, "Sé", "São Paulo", "SP"),
        cpf: new Cpf("39053344705"));
}
