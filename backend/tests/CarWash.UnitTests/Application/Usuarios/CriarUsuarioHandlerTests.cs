using System.Reflection;
using CarWash.Application.Abstractions;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Usuarios.Common;
using CarWash.Application.Usuarios.CriarUsuario;
using CarWash.Application.Usuarios.Persistence;
using CarWash.Domain.Entities;
using CarWash.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace CarWash.UnitTests.Application.Usuarios;

public class CriarUsuarioHandlerTests
{
    private readonly IUsuarioRepository _repo = Substitute.For<IUsuarioRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ICurrentRequestContext _contexto = Substitute.For<ICurrentRequestContext>();

    [Fact]
    public async Task Caminho_feliz_chama_hash_repo_e_retorna_dto_sem_senha()
    {
        _repo.ExisteComEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _hasher.Hash(Arg.Any<string>())
            .Returns("$argon2id$v=19$m=65536,t=3,p=1$c2FsdA$aGFzaA");

        var handler = NovoHandler();

        var resposta = await handler.HandleAsync(NovoComando(), CancellationToken.None);

        resposta.Should().NotBeNull();
        resposta.Nome.Should().Be("Alice Silva");
        resposta.Email.Should().Be("alice@carwash.local");
        resposta.Perfil.Should().Be(PerfilUsuario.Funcionario);
        resposta.Ativo.Should().BeTrue();
        resposta.Id.Should().NotBeEmpty();

        // O DTO não tem campo SenhaHash — confirmado pelo tipo.
        typeof(UsuarioResponse).GetProperty("SenhaHash").Should().BeNull();

        // Auditoria + ordem de chamadas
        _contexto.Received(1).DefinirEvento(CriarUsuarioHandler.EventoAuditoria);
        _hasher.Received(1).Hash("Senha1234");
        await _repo.Received(1).ExisteComEmailAsync("alice@carwash.local", Arg.Any<CancellationToken>());
        await _repo.Received(1).AdicionarAsync(
            Arg.Is<Usuario>(u =>
                u.EmailValor == "alice@carwash.local"
                && u.SenhaHash.StartsWith("$argon2id$", StringComparison.Ordinal)
                && u.Ativo),
            Arg.Any<CancellationToken>());
        await _repo.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Email_normalizado_para_lowercase_antes_de_persistir()
    {
        _repo.ExisteComEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _hasher.Hash(Arg.Any<string>()).Returns("$argon2id$x");

        var handler = NovoHandler();

        var cmd = NovoComando() with { Email = "Alice@CARWASH.Local" };
        var resposta = await handler.HandleAsync(cmd, CancellationToken.None);

        resposta.Email.Should().Be("alice@carwash.local");
    }

    [Fact]
    public async Task Pre_check_de_email_existente_lanca_ConflictException()
    {
        _repo.ExisteComEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = NovoHandler();

        var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.Which.Message.Should().Be(CriarUsuarioHandler.MensagemEmailDuplicado);
        ex.Which.Slug.Should().Be(CriarUsuarioHandler.SlugEmailDuplicado);

        await _repo.DidNotReceive().AdicionarAsync(Arg.Any<Usuario>(), Arg.Any<CancellationToken>());
        await _repo.DidNotReceive().SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Race_condition_DbUpdateException_com_UK_email_lanca_ConflictException()
    {
        _repo.ExisteComEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _hasher.Hash(Arg.Any<string>()).Returns("$argon2id$x");

        var pg = CriarPostgresExceptionUniqueViolation("uk_usuarios_email");
        _repo.SalvarAsync(Arg.Any<CancellationToken>())
            .Throws(new DbUpdateException("duplicate", pg));

        var handler = NovoHandler();

        var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.Which.Message.Should().Be(CriarUsuarioHandler.MensagemEmailDuplicado);
        ex.Which.Slug.Should().Be(CriarUsuarioHandler.SlugEmailDuplicado);
        ex.Which.InnerException.Should().BeOfType<DbUpdateException>();
    }

    [Fact]
    public async Task Race_condition_em_outra_constraint_propaga_DbUpdateException()
    {
        _repo.ExisteComEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _hasher.Hash(Arg.Any<string>()).Returns("$argon2id$x");

        var pg = CriarPostgresExceptionUniqueViolation("alguma_outra_constraint");
        _repo.SalvarAsync(Arg.Any<CancellationToken>())
            .Throws(new DbUpdateException("outro", pg));

        var handler = NovoHandler();

        var act = () => handler.HandleAsync(NovoComando(), CancellationToken.None);

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Email_malformado_no_command_lanca_ValidationException()
    {
        _repo.ExisteComEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var handler = NovoHandler();

        var cmd = NovoComando() with { Email = "naoeemail" };
        var act = () => handler.HandleAsync(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    private CriarUsuarioHandler NovoHandler() =>
        new(_repo, _hasher, _contexto, NullLogger<CriarUsuarioHandler>.Instance);

    private static CriarUsuarioCommand NovoComando() => new(
        Nome: "Alice Silva",
        Email: "alice@carwash.local",
        Senha: "Senha1234",
        Perfil: PerfilUsuario.Funcionario);

    /// <summary>
    /// Constrói uma <see cref="PostgresException"/> com SqlState=23505 e ConstraintName
    /// arbitrário. Em Npgsql 8 a propriedade <c>ConstraintName</c> tem setter interno;
    /// é necessário acessá-lo via reflection para popular o cenário de teste.
    /// </summary>
    private static PostgresException CriarPostgresExceptionUniqueViolation(string constraintName)
    {
        var ex = new PostgresException(
            messageText: "duplicate key value violates unique constraint",
            severity: "ERROR",
            invariantSeverity: "ERROR",
            sqlState: "23505");

        var prop = typeof(PostgresException).GetProperty(
            nameof(PostgresException.ConstraintName),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (prop is not null && prop.CanWrite)
        {
            prop.SetValue(ex, constraintName);
        }
        else
        {
            // Fallback para backing field do auto-property gerado pelo compilador.
            var campo = typeof(PostgresException).GetField(
                "<ConstraintName>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            campo?.SetValue(ex, constraintName);
        }

        return ex;
    }
}
