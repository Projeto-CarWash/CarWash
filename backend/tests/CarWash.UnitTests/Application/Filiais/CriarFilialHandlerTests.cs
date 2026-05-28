using CarWash.Application.Abstractions;
using CarWash.Application.Filiais.Common;
using CarWash.Application.Filiais.CriarFilial;
using CarWash.Application.Filiais.Persistence;
using CarWash.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace CarWash.UnitTests.Application.Filiais;

/// <summary>
/// RF018 — use case de criação de filial. Mocks de repo/auditoria/contexto isolam
/// a Application das dependências; sem I/O real.
/// </summary>
public class CriarFilialHandlerTests
{
    private readonly IFilialRepository _repo = Substitute.For<IFilialRepository>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();
    private readonly ICurrentRequestContext _ctx = Substitute.For<ICurrentRequestContext>();

    [Fact]
    public async Task Caminho_feliz_cria_persiste_audita_e_retorna_response()
    {
        _repo.ExisteComNomeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        Filial? adicionada = null;
        await _repo.AdicionarAsync(Arg.Do<Filial>(f => adicionada = f), Arg.Any<CancellationToken>());

        var handler = NovoHandler();
        var resposta = await handler.HandleAsync(NovoComando(), CancellationToken.None);

        // Domínio criado e devolvido na resposta.
        resposta.Id.Should().NotBeEmpty();
        resposta.Nome.Should().Be("Filial Centro");
        resposta.CelulasAtivas.Should().Be(4);
        resposta.Ativa.Should().BeTrue();
        adicionada.Should().NotBeNull();
        resposta.Id.Should().Be(adicionada!.Id);

        // Evento definido ANTES do SaveChanges (para o interceptor capturar o INSERT).
        _ctx.Received(1).DefinirEvento(CriarFilialHandler.EventoAuditoria);
        await _repo.Received(1).ExisteComNomeAsync("Filial Centro", Arg.Any<CancellationToken>());
        await _repo.Received(1).AdicionarAsync(Arg.Any<Filial>(), Arg.Any<CancellationToken>());
        await _repo.Received(1).SalvarAsync(Arg.Any<CancellationToken>());

        // Auditoria explícita com evento/entidade corretos.
        await _audit.Received(1).LogAsync(
            CriarFilialHandler.EventoAuditoria,
            CriarFilialHandler.EntidadeAuditoria,
            adicionada.Id,
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Nome_normalizado_com_trim_antes_de_persistir()
    {
        _repo.ExisteComNomeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        Filial? adicionada = null;
        await _repo.AdicionarAsync(Arg.Do<Filial>(f => adicionada = f), Arg.Any<CancellationToken>());

        var handler = NovoHandler();
        var resposta = await handler.HandleAsync(NovoComando() with { Nome = "  Filial Centro  " }, CancellationToken.None);

        resposta.Nome.Should().Be("Filial Centro");
        adicionada!.Nome.Should().Be("Filial Centro");
        await _repo.Received(1).ExisteComNomeAsync("Filial Centro", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Pre_check_de_nome_duplicado_lanca_NomeFilialJaExisteException()
    {
        _repo.ExisteComNomeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<NomeFilialJaExisteException>();
        ex.Which.Message.Should().Be(NomeFilialJaExisteException.MensagemPadrao);
        ex.Which.Slug.Should().Be(NomeFilialJaExisteException.SlugPadrao);

        // Não persiste nem audita o evento explícito quando o pré-check barra.
        await _repo.DidNotReceive().AdicionarAsync(Arg.Any<Filial>(), Arg.Any<CancellationToken>());
        await _repo.DidNotReceive().SalvarAsync(Arg.Any<CancellationToken>());
        await _audit.DidNotReceive().LogAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Race_condition_no_repositorio_propaga_NomeFilialJaExiste()
    {
        // A tradução de DbUpdateException → NomeFilialJaExisteException vive na
        // Infrastructure (FilialRepository.SalvarAsync). O handler apenas a propaga.
        _repo.ExisteComNomeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _repo.SalvarAsync(Arg.Any<CancellationToken>()).Throws(new NomeFilialJaExisteException());

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<NomeFilialJaExisteException>();
        ex.Which.Slug.Should().Be(NomeFilialJaExisteException.SlugPadrao);
    }

    private CriarFilialHandler NovoHandler() =>
        new(_repo, _audit, _ctx, NullLogger<CriarFilialHandler>.Instance);

    private static CriarFilialCommand NovoComando() => new(
        Nome: "Filial Centro",
        CelulasAtivas: 4,
        Timezone: "America/Sao_Paulo");
}
