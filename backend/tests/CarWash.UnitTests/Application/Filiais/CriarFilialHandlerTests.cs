using CarWash.Application.Abstractions;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Filiais.Common;
using CarWash.Application.Filiais.Criar;
using CarWash.Application.Filiais.Persistence;
using CarWash.Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CarWash.UnitTests.Application.Filiais;

public class CriarFilialHandlerTests
{
    private readonly IFilialRepository _repo = Substitute.For<IFilialRepository>();
    private readonly ICurrentRequestContext _ctx = Substitute.For<ICurrentRequestContext>();

    [Fact]
    public async Task Caminho_feliz_chama_repo_e_retorna_response_com_traceId()
    {
        _repo.ExisteCodigoAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _repo.ExisteCnpjAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _repo.ExisteNomeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var handler = NovoHandler();
        var resposta = await handler.HandleAsync(NovoComando(), CancellationToken.None);

        resposta.Id.Should().NotBeEmpty();
        resposta.TraceId.Should().Be("trace-1");
        resposta.Mensagem.Should().Be("Filial cadastrada com sucesso.");

        _ctx.Received(1).DefinirEvento(CriarFilialHandler.EventoAuditoria);

        await _repo.Received(1).AdicionarAsync(
            Arg.Is<Filial>(f => f.Nome == "Filial Matriz" && f.Codigo == "MTZ01"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Codigo_duplicado_lanca_exception_com_slug_correto()
    {
        _repo.ExisteCodigoAsync("MTZ01", Arg.Any<CancellationToken>()).Returns(true);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<FilialCodigoJaExisteException>();
        ex.Which.Slug.Should().Be(FilialCodigoJaExisteException.SlugPadrao);
    }

    [Fact]
    public async Task Cnpj_duplicado_lanca_exception_com_slug_correto()
    {
        _repo.ExisteCodigoAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _repo.ExisteCnpjAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<FilialCnpjJaExisteException>();
        ex.Which.Slug.Should().Be(FilialCnpjJaExisteException.SlugPadrao);
    }

    [Fact]
    public async Task Nome_duplicado_lanca_exception_com_slug_correto()
    {
        _repo.ExisteCodigoAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _repo.ExisteCnpjAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _repo.ExisteNomeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<FilialNomeJaExisteException>();
        ex.Which.Slug.Should().Be(FilialNomeJaExisteException.SlugPadrao);
    }

    [Fact]
    public async Task Codigo_lowercase_eh_normalizado_para_upper_antes_de_persistir()
    {
        _repo.ExisteCodigoAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _repo.ExisteCnpjAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _repo.ExisteNomeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var handler = NovoHandler();
        await handler.HandleAsync(NovoComando() with { Codigo = "mtz01" }, CancellationToken.None);

        await _repo.Received(1).AdicionarAsync(
            Arg.Is<Filial>(f => f.Codigo == "MTZ01"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CelulasAtivas_null_dispara_ValidationException_com_chave_celulasAtivas()
    {
        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando() with { CelulasAtivas = null }, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Erros.Should().ContainKey("celulasAtivas");
    }

    private CriarFilialHandler NovoHandler() => new(_repo, _ctx);

    private static CriarFilialCommand NovoComando() => new(
        Nome: "Filial Matriz",
        Codigo: "MTZ01",
        Cnpj: "11222333000181",
        CelulasAtivas: 30,
        Timezone: null,
        Endereco: null,
        TraceId: "trace-1",
        UsuarioId: null);
}
