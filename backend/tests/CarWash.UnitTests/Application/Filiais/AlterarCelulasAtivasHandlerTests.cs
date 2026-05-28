using CarWash.Application.Abstractions;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Filiais.AlterarCelulasAtivas;
using CarWash.Application.Filiais.Persistence;
using CarWash.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace CarWash.UnitTests.Application.Filiais;

/// <summary>
/// RF018 — use case de alteração de células ativas. Cobre o caminho de mudança
/// (persiste + audita) e a idempotência (no-op sem save/audit), além do 404.
/// </summary>
public class AlterarCelulasAtivasHandlerTests
{
    private readonly IFilialRepository _repo = Substitute.For<IFilialRepository>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();
    private readonly ICurrentRequestContext _ctx = Substitute.For<ICurrentRequestContext>();

    [Fact]
    public async Task Valor_diferente_ajusta_persiste_audita_e_retorna_response()
    {
        var filial = NovaFilial(celulas: 4);
        _repo.ObterPorIdAsync(filial.Id, Arg.Any<CancellationToken>()).Returns(filial);

        var handler = NovoHandler();
        var resposta = await handler.HandleAsync(new AlterarCelulasAtivasCommand(filial.Id, 7), CancellationToken.None);

        resposta.Id.Should().Be(filial.Id);
        resposta.CelulasAtivas.Should().Be(7);
        filial.CelulasAtivas.Should().Be(7);

        await _repo.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
        _ctx.Received(1).DefinirEvento(AlterarCelulasAtivasHandler.EventoAuditoria);
        await _audit.Received(1).LogAsync(
            AlterarCelulasAtivasHandler.EventoAuditoria,
            AlterarCelulasAtivasHandler.EntidadeAuditoria,
            filial.Id,
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Valor_igual_ao_atual_e_idempotente_sem_save_sem_audit()
    {
        var filial = NovaFilial(celulas: 4);
        _repo.ObterPorIdAsync(filial.Id, Arg.Any<CancellationToken>()).Returns(filial);

        var handler = NovoHandler();
        var resposta = await handler.HandleAsync(new AlterarCelulasAtivasCommand(filial.Id, 4), CancellationToken.None);

        resposta.CelulasAtivas.Should().Be(4);
        filial.CelulasAtivas.Should().Be(4);

        // No-op: nenhum SaveChanges, nenhum DefinirEvento, nenhum IAuditLogger.
        await _repo.DidNotReceive().SalvarAsync(Arg.Any<CancellationToken>());
        _ctx.DidNotReceive().DefinirEvento(Arg.Any<string>());
        await _audit.DidNotReceive().LogAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Filial_inexistente_lanca_NotFoundException_com_mensagem_exata()
    {
        _repo.ObterPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Filial?)null);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(new AlterarCelulasAtivasCommand(Guid.NewGuid(), 7), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.Message.Should().Be(AlterarCelulasAtivasHandler.MensagemNaoEncontrado);

        await _repo.DidNotReceive().SalvarAsync(Arg.Any<CancellationToken>());
    }

    private AlterarCelulasAtivasHandler NovoHandler() =>
        new(_repo, _audit, _ctx, NullLogger<AlterarCelulasAtivasHandler>.Instance);

    private static Filial NovaFilial(int celulas) =>
        Filial.Criar(Guid.NewGuid(), "Filial Centro", "FC01", celulas, timezone: "America/Sao_Paulo");
}
