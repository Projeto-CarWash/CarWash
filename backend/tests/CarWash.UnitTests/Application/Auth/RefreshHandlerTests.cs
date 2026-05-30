using CarWash.Application.Abstractions;
using CarWash.Application.Auth.Abstractions;
using CarWash.Application.Auth.Persistence;
using CarWash.Application.Auth.Refresh;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Usuarios.Persistence;
using CarWash.Domain.Entities;
using CarWash.Domain.Enums;
using CarWash.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace CarWash.UnitTests.Application.Auth;

public class RefreshHandlerTests
{
    private readonly IRefreshTokenService _refresh = Substitute.For<IRefreshTokenService>();
    private readonly IAccessTokenService _access = Substitute.For<IAccessTokenService>();
    private readonly IUsuarioRepository _repo = Substitute.For<IUsuarioRepository>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();
    private readonly ICurrentRequestContext _ctx = Substitute.For<ICurrentRequestContext>();

    [Fact]
    public async Task Token_invalido_lanca_RefreshTokenInvalido()
    {
        _refresh.ValidarParaRotacaoAsync("ruim", Arg.Any<CancellationToken>())
            .Returns<Task<RotacaoContexto>>(_ => throw new RefreshTokenInvalidoException());

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(new RefreshCommand("ruim"), CancellationToken.None);

        await act.Should().ThrowAsync<RefreshTokenInvalidoException>();
    }

    [Fact]
    public async Task Usuario_inativo_revoga_sessao_e_lanca_RefreshTokenInvalido()
    {
        var usuario = NovoUsuario(ativo: false);
        var sessao = NovaSessao(usuario.Id);
        var transacao = Substitute.For<IUsuarioSessaoTransacao>();

        _refresh.ValidarParaRotacaoAsync("bom", Arg.Any<CancellationToken>())
            .Returns(new RotacaoContexto(sessao, transacao));
        _repo.ObterPorIdAsync(usuario.Id, Arg.Any<CancellationToken>()).Returns(usuario);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(new RefreshCommand("bom"), CancellationToken.None);

        await act.Should().ThrowAsync<RefreshTokenInvalidoException>();
        await _refresh.Received(1).RevogarAsync("bom", Arg.Any<CancellationToken>());
        await transacao.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await transacao.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task Refresh_valido_rotaciona_sessao_e_retorna_novo_access()
    {
        var usuario = NovoUsuario(ativo: true);
        var sessaoAtual = NovaSessao(usuario.Id);
        var transacao = Substitute.For<IUsuarioSessaoTransacao>();

        _refresh.ValidarParaRotacaoAsync("antigo", Arg.Any<CancellationToken>())
            .Returns(new RotacaoContexto(sessaoAtual, transacao));
        _repo.ObterPorIdAsync(usuario.Id, Arg.Any<CancellationToken>()).Returns(usuario);
        _access.Emitir(usuario).Returns(("novo-jwt", DateTime.UtcNow.AddMinutes(15)));
        _refresh.EmitirAsync(usuario, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RefreshTokenEmitido("novo-refresh", DateTime.UtcNow.AddDays(7), Guid.NewGuid())));

        var handler = NovoHandler();
        var resp = await handler.HandleAsync(new RefreshCommand("antigo"), CancellationToken.None);

        resp.AccessToken.Should().Be("novo-jwt");
        resp.RefreshToken.Should().Be("novo-refresh");
        resp.Usuario.Id.Should().Be(usuario.Id);

        // Rotação: revoga o antigo (uma vez no fluxo válido).
        await _refresh.Received(1).RevogarAsync("antigo", Arg.Any<CancellationToken>());
        await _refresh.Received(1).EmitirAsync(usuario, Arg.Any<CancellationToken>());
        await transacao.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await transacao.Received(1).DisposeAsync();

        await _audit.Received(1).LogAsync(
            RefreshHandler.EventoSucesso,
            RefreshHandler.EntidadeAuditoria,
            Arg.Any<Guid>(),
            Arg.Any<object?>(),
            Arg.Any<CancellationToken>());
    }

    private RefreshHandler NovoHandler() =>
        new(_refresh, _access, _repo, _audit, _ctx, NullLogger<RefreshHandler>.Instance);

    private static Usuario NovoUsuario(bool ativo)
    {
        var u = Usuario.Criar(
            id: Guid.NewGuid(),
            nome: "Bob",
            email: new Email("bob@carwash.local"),
            senhaHash: "$argon2id$v=19$m=65536,t=3,p=1$c2FsdA$aGFzaA",
            perfil: PerfilUsuario.Funcionario);

        if (!ativo)
        {
            u.Inativar();
        }

        return u;
    }

    private static UsuarioSessao NovaSessao(Guid usuarioId) =>
        UsuarioSessao.Criar(
            id: Guid.NewGuid(),
            usuarioId: usuarioId,
            refreshTokenHash: "hash",
            expiraEm: DateTime.UtcNow.AddDays(7),
            ipOrigem: null,
            userAgent: null);
}
