using CarWash.Application.Abstractions;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Usuarios.AlterarStatus;
using CarWash.Application.Usuarios.Persistence;
using CarWash.Domain.Entities;
using CarWash.Domain.Enums;
using CarWash.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace CarWash.UnitTests.Application.Usuarios;

public class AlterarStatusUsuarioHandlerTests
{
    private readonly IUsuarioRepository _repo = Substitute.For<IUsuarioRepository>();
    private readonly ICurrentRequestContext _contexto = Substitute.For<ICurrentRequestContext>();

    [Fact]
    public async Task Ativar_usuario_inativo_persiste_audita_e_retorna_200()
    {
        var usuario = NovoUsuario(ativo: false);
        _repo.ObterPorIdRastreadoAsync(usuario.Id, Arg.Any<CancellationToken>()).Returns(usuario);

        var handler = NovoHandler();
        var resp = await handler.HandleAsync(new AlterarStatusUsuarioCommand(usuario.Id, true), CancellationToken.None);

        resp.Id.Should().Be(usuario.Id);
        resp.Ativo.Should().BeTrue();
        usuario.Ativo.Should().BeTrue();

        await _repo.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
        _contexto.Received(1).DefinirEvento(AlterarStatusUsuarioHandler.EventoAuditoria);
    }

    [Fact]
    public async Task Inativar_usuario_ativo_persiste_audita_e_retorna_200()
    {
        var usuario = NovoUsuario(ativo: true);
        _repo.ObterPorIdRastreadoAsync(usuario.Id, Arg.Any<CancellationToken>()).Returns(usuario);

        var handler = NovoHandler();
        var resp = await handler.HandleAsync(new AlterarStatusUsuarioCommand(usuario.Id, false), CancellationToken.None);

        resp.Ativo.Should().BeFalse();
        usuario.Ativo.Should().BeFalse();

        await _repo.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Idempotente_sem_save_sem_audit_retorna_estado_atual()
    {
        var usuario = NovoUsuario(ativo: true);
        _repo.ObterPorIdRastreadoAsync(usuario.Id, Arg.Any<CancellationToken>()).Returns(usuario);

        var handler = NovoHandler();
        var resp = await handler.HandleAsync(new AlterarStatusUsuarioCommand(usuario.Id, true), CancellationToken.None);

        resp.Ativo.Should().BeTrue();
        usuario.Ativo.Should().BeTrue();

        await _repo.DidNotReceive().SalvarAsync(Arg.Any<CancellationToken>());
        _contexto.DidNotReceive().DefinirEvento(Arg.Any<string>());
    }

    [Fact]
    public async Task Idempotente_para_inativo_tambem()
    {
        var usuario = NovoUsuario(ativo: false);
        _repo.ObterPorIdRastreadoAsync(usuario.Id, Arg.Any<CancellationToken>()).Returns(usuario);

        var handler = NovoHandler();
        var resp = await handler.HandleAsync(new AlterarStatusUsuarioCommand(usuario.Id, false), CancellationToken.None);

        resp.Ativo.Should().BeFalse();
        await _repo.DidNotReceive().SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Id_inexistente_lanca_NotFoundException()
    {
        _repo.ObterPorIdRastreadoAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Usuario?)null);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(new AlterarStatusUsuarioCommand(Guid.NewGuid(), false), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.Message.Should().Be(AlterarStatusUsuarioHandler.MensagemNaoEncontrado);

        await _repo.DidNotReceive().SalvarAsync(Arg.Any<CancellationToken>());
    }

    private AlterarStatusUsuarioHandler NovoHandler() =>
        new(_repo, _contexto, NullLogger<AlterarStatusUsuarioHandler>.Instance);

    private static Usuario NovoUsuario(bool ativo)
    {
        var u = Usuario.Criar(
            id: Guid.NewGuid(),
            nome: "Alice Silva",
            email: new Email("alice@carwash.local"),
            senhaHash: "$argon2id$v=19$m=65536,t=3,p=1$c2FsdA$aGFzaA",
            perfil: PerfilUsuario.Funcionario);

        if (!ativo)
        {
            u.Inativar();
        }

        return u;
    }
}
