using CarWash.Application.Abstractions;
using CarWash.Application.Auth.Abstractions;
using CarWash.Application.Auth.Logout;
using CarWash.Application.Auth.Persistence;
using CarWash.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace CarWash.UnitTests.Application.Auth;

public class LogoutHandlerTests
{
    private static readonly Guid UsuarioId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly Guid SessaoId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    private readonly IRefreshTokenService _refresh = Substitute.For<IRefreshTokenService>();
    private readonly IUsuarioSessaoRepository _sessoes = Substitute.For<IUsuarioSessaoRepository>();
    private readonly ITokenHasher _hasher = Substitute.For<ITokenHasher>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();
    private readonly ICurrentRequestContext _ctx = Substitute.For<ICurrentRequestContext>();

    [Fact]
    public async Task Token_ausente_nao_chama_RevogarAsync_nem_audita()
    {
        var handler = NovoHandler();
        await handler.HandleAsync(new LogoutCommand(null), CancellationToken.None);

        await _refresh.DidNotReceive().RevogarAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _audit.DidNotReceive().LogAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Guid?>(),
            Arg.Any<object?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Token_desconhecido_nao_chama_RevogarAsync_nem_audita()
    {
        _hasher.Hash("forjado").Returns("hash-forjado");
        _sessoes.ObterPorHashAsync("hash-forjado", Arg.Any<CancellationToken>())
            .Returns((UsuarioSessao?)null);

        var handler = NovoHandler();
        await handler.HandleAsync(new LogoutCommand("forjado"), CancellationToken.None);

        await _refresh.DidNotReceive().RevogarAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _audit.DidNotReceive().LogAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Guid?>(),
            Arg.Any<object?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Token_presente_revoga_e_audita_com_sessao()
    {
        _hasher.Hash("tok").Returns("hash-tok");
        var sessao = UsuarioSessao.Criar(
            id: SessaoId,
            usuarioId: UsuarioId,
            refreshTokenHash: "hash-tok",
            expiraEm: DateTime.UtcNow.AddDays(7));
        _sessoes.ObterPorHashAsync("hash-tok", Arg.Any<CancellationToken>()).Returns(sessao);

        var handler = NovoHandler();
        await handler.HandleAsync(new LogoutCommand("tok"), CancellationToken.None);

        await _refresh.Received(1).RevogarAsync("tok", Arg.Any<CancellationToken>());
        await _audit.Received(1).LogAsync(
            LogoutHandler.EventoSucesso,
            LogoutHandler.EntidadeAuditoria,
            SessaoId,
            null,
            Arg.Any<CancellationToken>());
        _ctx.Received(1).DefinirEvento(LogoutHandler.EventoSucesso);
    }

    private LogoutHandler NovoHandler() =>
        new(_refresh, _sessoes, _hasher, _audit, _ctx, NullLogger<LogoutHandler>.Instance);
}
