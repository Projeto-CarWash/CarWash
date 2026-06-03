using CarWash.Application.Clientes.Common;
using CarWash.Application.Clientes.Criar;
using CarWash.Application.Clientes.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.DTOs;
using CarWash.Application.Interfaces;
using CarWash.Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CarWash.UnitTests.Application.Clientes;

public class CriarClienteHandlerTests
{
    private readonly IClienteRepository _repo = Substitute.For<IClienteRepository>();
    private readonly IVeiculoService _veiculoService = Substitute.For<IVeiculoService>();

    [Fact]
    public async Task Caminho_feliz_chama_repo_e_retorna_response_com_traceId()
    {
        _repo.ExisteCpfAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _repo.ExisteEmailAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);

        var handler = NovoHandler();
        var resposta = await handler.HandleAsync(NovoComando(), CancellationToken.None);

        resposta.Id.Should().NotBeEmpty();
        resposta.TraceId.Should().Be("trace-1");
        resposta.Mensagem.Should().Be("Cliente cadastrado com sucesso.");

        await _repo.Received(1).AdicionarAsync(
            Arg.Is<Cliente>(c => c.Nome == "Maria Souza" && c.Cpf == "39053344705"),
            "trace-1",
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cpf_duplicado_lanca_ConflictException()
    {
        _repo.ExisteCpfAsync("39053344705", Arg.Any<CancellationToken>()).Returns(true);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.Which.Slug.Should().Be("cliente-documento-duplicado");
    }

    [Fact]
    public async Task Email_duplicado_lanca_ConflictException()
    {
        _repo.ExisteCpfAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _repo.ExisteEmailAsync("maria@x.com", null, Arg.Any<CancellationToken>()).Returns(true);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando() with { Email = "maria@x.com" }, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.Which.Slug.Should().Be("cliente-email-duplicado");
    }

    [Fact]
    public async Task DataNascimento_null_dispara_ValidationException()
    {
        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando() with { DataNascimento = null }, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    private CriarClienteHandler NovoHandler() => new(_repo, _veiculoService);

    private static CriarClienteCommand NovoComando() => new(
        Nome: "Maria Souza",
        DataNascimento: new DateOnly(1990, 1, 1),
        Cpf: "39053344705",
        Cnpj: null,
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
        Veiculos: null,
        Observacoes: null,
        TraceId: "trace-1",
        UsuarioId: null);
}
