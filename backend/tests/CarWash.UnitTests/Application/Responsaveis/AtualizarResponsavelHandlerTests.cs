using System.Text.Json;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Responsaveis.Atualizar;
using CarWash.Application.Responsaveis.Persistence;
using CarWash.Domain.Entities;
using CarWash.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace CarWash.UnitTests.Application.Responsaveis;

public class AtualizarResponsavelHandlerTests
{
    private readonly IResponsavelRepository _repositorio = Substitute.For<IResponsavelRepository>();

    [Fact]
    public async Task Caminho_feliz_atualiza_dados_persiste_e_retorna_response()
    {
        var responsavel = NovoResponsavel();
        _repositorio.ObterPorIdRastreadoAsync(responsavel.Id, responsavel.ClienteTitularId, Arg.Any<CancellationToken>())
            .Returns(responsavel);

        var handler = NovoHandler();
        var comando = NovoComando(responsavel) with
        {
            Nome = "Maria Atualizada",
            Telefone = "11912345678",
            Email = "maria@x.com",
            GrauVinculo = "PROCURADOR",
        };

        var resposta = await handler.HandleAsync(comando, CancellationToken.None);

        resposta.ResponsavelId.Should().Be(responsavel.Id);
        resposta.Nome.Should().Be("Maria Atualizada");
        resposta.Telefone.Should().Be("11912345678");
        resposta.Email.Should().Be("maria@x.com");
        resposta.GrauVinculo.Should().Be("PROCURADOR");
        resposta.Documento.Should().Be(responsavel.Documento, "documento é imutável no PUT");

        await _repositorio.Received(1).SalvarAsync("trace-1", comando.UsuarioId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Responsavel_inexistente_lanca_NotFoundException()
    {
        _repositorio.ObterPorIdRastreadoAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Responsavel?)null);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(NovoResponsavel()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _repositorio.DidNotReceive().SalvarAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Campos_extras_nao_editaveis_sao_ignorados_sem_alterar_documento()
    {
        var responsavel = NovoResponsavel();
        string documentoOriginal = responsavel.Documento;
        _repositorio.ObterPorIdRastreadoAsync(responsavel.Id, responsavel.ClienteTitularId, Arg.Any<CancellationToken>())
            .Returns(responsavel);

        var extras = new Dictionary<string, JsonElement>
        {
            ["documento"] = JsonSerializer.SerializeToElement("99999999999"),
            ["ativo"] = JsonSerializer.SerializeToElement(false),
        };

        var handler = NovoHandler();
        var resposta = await handler.HandleAsync(
            NovoComando(responsavel) with { CamposExtras = extras },
            CancellationToken.None);

        resposta.Documento.Should().Be(documentoOriginal);
        resposta.Ativo.Should().BeTrue("campo 'ativo' só muda pelo PATCH /status");
    }

    [Fact]
    public async Task Telefone_e_email_nulos_limpam_os_campos_opcionais()
    {
        var responsavel = NovoResponsavel();
        _repositorio.ObterPorIdRastreadoAsync(responsavel.Id, responsavel.ClienteTitularId, Arg.Any<CancellationToken>())
            .Returns(responsavel);

        var handler = NovoHandler();
        var resposta = await handler.HandleAsync(
            NovoComando(responsavel) with { Telefone = null, Email = null },
            CancellationToken.None);

        resposta.Telefone.Should().BeNull();
        resposta.Email.Should().BeNull();
    }

    private AtualizarResponsavelHandler NovoHandler() =>
        new(_repositorio, NullLogger<AtualizarResponsavelHandler>.Instance);

    private static AtualizarResponsavelCommand NovoComando(Responsavel responsavel) => new(
        ResponsavelId: responsavel.Id,
        ClienteTitularId: responsavel.ClienteTitularId,
        Nome: "Nome Valido",
        Telefone: "11987654321",
        Email: "novo@x.com",
        GrauVinculo: "RESPONSAVEL_FINANCEIRO",
        CamposExtras: null,
        TraceId: "trace-1",
        UsuarioId: Guid.NewGuid());

    private static Responsavel NovoResponsavel() => Responsavel.Criar(
        id: Guid.NewGuid(),
        clienteTitularId: Guid.NewGuid(),
        nome: "João Original",
        documento: "39053344705",
        grauVinculo: GrauVinculo.ResponsavelFinanceiro);
}
