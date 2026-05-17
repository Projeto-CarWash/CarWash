using CarWash.Application.Abstractions;
using CarWash.Application.Auth.Abstractions;
using CarWash.Application.Auth.Logout;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace CarWash.UnitTests.Application.Auth;

public class LogoutHandlerTests
{
    private readonly IRefreshTokenService _refresh = Substitute.For<IRefreshTokenService>();
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
    public async Task Token_presente_revoga_e_audita()
    {
        var handler = NovoHandler();
        await handler.HandleAsync(new LogoutCommand("tok"), CancellationToken.None);

        await _refresh.Received(1).RevogarAsync("tok", Arg.Any<CancellationToken>());
        await _audit.Received(1).LogAsync(
            LogoutHandler.EventoSucesso,
            LogoutHandler.EntidadeAuditoria,
            null,
            null,
            Arg.Any<CancellationToken>());
    }

    private LogoutHandler NovoHandler() =>
        new(_refresh, _audit, _ctx, NullLogger<LogoutHandler>.Instance);
}
