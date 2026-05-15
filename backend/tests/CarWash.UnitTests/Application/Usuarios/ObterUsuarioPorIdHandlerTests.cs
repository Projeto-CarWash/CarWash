using CarWash.Application.Common.Exceptions;
using CarWash.Application.Usuarios.ObterUsuarioPorId;
using CarWash.Application.Usuarios.Persistence;
using CarWash.Domain.Entities;
using CarWash.Domain.Enums;
using CarWash.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CarWash.UnitTests.Application.Usuarios;

public class ObterUsuarioPorIdHandlerTests
{
    private readonly IUsuarioRepository _repo = Substitute.For<IUsuarioRepository>();

    [Fact]
    public async Task Existente_retorna_dto_sem_senha_hash()
    {
        var usuario = Usuario.Criar(
            id: Guid.NewGuid(),
            nome: "Bob",
            email: new Email("bob@carwash.local"),
            senhaHash: "$argon2id$v=19$m=65536,t=3,p=1$c2FsdA$aGFzaA",
            perfil: PerfilUsuario.Admin);

        _repo.ObterPorIdAsync(usuario.Id, Arg.Any<CancellationToken>()).Returns(usuario);

        var handler = new ObterUsuarioPorIdHandler(_repo);

        var resposta = await handler.HandleAsync(new ObterUsuarioPorIdQuery(usuario.Id), CancellationToken.None);

        resposta.Id.Should().Be(usuario.Id);
        resposta.Nome.Should().Be("Bob");
        resposta.Email.Should().Be("bob@carwash.local");
        resposta.Perfil.Should().Be(PerfilUsuario.Admin);
        resposta.Ativo.Should().BeTrue();
    }

    [Fact]
    public async Task Inexistente_lanca_NotFoundException()
    {
        _repo.ObterPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Usuario?)null);

        var handler = new ObterUsuarioPorIdHandler(_repo);

        var act = () => handler.HandleAsync(new ObterUsuarioPorIdQuery(Guid.NewGuid()), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.Message.Should().Be(ObterUsuarioPorIdHandler.MensagemNaoEncontrado);
    }
}
