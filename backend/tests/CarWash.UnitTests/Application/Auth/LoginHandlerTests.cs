using CarWash.Application.Abstractions;
using CarWash.Application.Auth.Abstractions;
using CarWash.Application.Auth.Login;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Common.Security;
using CarWash.Application.Usuarios.Persistence;
using CarWash.Domain.Entities;
using CarWash.Domain.Enums;
using CarWash.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace CarWash.UnitTests.Application.Auth;

public class LoginHandlerTests
{
    private const string DummyHash = "$argon2id$v=19$m=65536,t=3,p=1$ZHVtbXk$ZHVtbXk";

    private readonly IUsuarioRepository _repo = Substitute.For<IUsuarioRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IAuthTokenService _tokens = Substitute.For<IAuthTokenService>();
    private readonly IAuditLogger _auditoria = Substitute.For<IAuditLogger>();
    private readonly ICurrentRequestContext _contexto = Substitute.For<ICurrentRequestContext>();
    private readonly DummyPasswordHash _dummy = new(DummyHash);

    [Fact]
    public async Task Email_inexistente_lanca_InvalidCredentials_e_audit_sem_usuarioId()
    {
        _repo.ObterPorEmailAsync("alice@carwash.local", Arg.Any<CancellationToken>())
            .Returns((Usuario?)null);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(new LoginCommand("alice@carwash.local", "Senha1234"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();

        _hasher.Received(1).Verify("Senha1234", DummyHash);

        await _auditoria.Received(1).LogAsync(
            LoginHandler.EventoFalha,
            LoginHandler.EntidadeAuditoria,
            null,
            Arg.Is<object>(o => DadosTem(o, "Motivo", LoginHandler.MotivoCredencialInvalida)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Senha_errada_para_usuario_existente_lanca_InvalidCredentials_incrementa_contador()
    {
        var usuario = NovoUsuario(ativo: true);
        _repo.ObterPorEmailAsync(usuario.EmailValor, Arg.Any<CancellationToken>()).Returns(usuario);
        _hasher.Verify("ErradaXYZ", usuario.SenhaHash).Returns(false);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(new LoginCommand(usuario.EmailValor, "ErradaXYZ"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();

        usuario.TentativasInvalidas.Should().Be(1);
        usuario.BloqueadoAte.Should().BeNull();
        await _repo.Received(1).SalvarAsync(Arg.Any<CancellationToken>());

        // entidadeId carregado para rastreabilidade no audit log (mensagem HTTP segue unificada
        // — quem garante anti-enumeration é o handler, não o audit log interno).
        await _auditoria.Received(1).LogAsync(
            LoginHandler.EventoFalha,
            LoginHandler.EntidadeAuditoria,
            usuario.Id,
            Arg.Is<object>(o => DadosTem(o, "Motivo", LoginHandler.MotivoCredencialInvalida)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Inativo_com_senha_correta_lanca_UsuarioInativo()
    {
        var usuario = NovoUsuario(ativo: false);
        _repo.ObterPorEmailAsync(usuario.EmailValor, Arg.Any<CancellationToken>()).Returns(usuario);
        _hasher.Verify("Senha1234", usuario.SenhaHash).Returns(true);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(new LoginCommand(usuario.EmailValor, "Senha1234"), CancellationToken.None);

        await act.Should().ThrowAsync<UsuarioInativoException>();

        await _auditoria.Received(1).LogAsync(
            LoginHandler.EventoFalha,
            LoginHandler.EntidadeAuditoria,
            usuario.Id,
            Arg.Is<object>(o => DadosTem(o, "Motivo", LoginHandler.MotivoUsuarioInativo)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Inativo_com_senha_errada_lanca_InvalidCredentials_e_NAO_UsuarioInativo()
    {
        var usuario = NovoUsuario(ativo: false);
        _repo.ObterPorEmailAsync(usuario.EmailValor, Arg.Any<CancellationToken>()).Returns(usuario);
        _hasher.Verify("ErradaXYZ", usuario.SenhaHash).Returns(false);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(new LoginCommand(usuario.EmailValor, "ErradaXYZ"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();

        // Não deve ter audit de UsuarioInativo (credencial inválida tem precedência).
        await _auditoria.DidNotReceive().LogAsync(
            LoginHandler.EventoFalha,
            LoginHandler.EntidadeAuditoria,
            usuario.Id,
            Arg.Is<object>(o => DadosTem(o, "Motivo", LoginHandler.MotivoUsuarioInativo)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Ativo_com_senha_correta_retorna_LoginResponse_com_token()
    {
        var usuario = NovoUsuario(ativo: true);
        _repo.ObterPorEmailAsync(usuario.EmailValor, Arg.Any<CancellationToken>()).Returns(usuario);
        _hasher.Verify("Senha1234", usuario.SenhaHash).Returns(true);
        _hasher.NeedsRehash(usuario.SenhaHash).Returns(false);

        var expires = DateTime.UtcNow.AddHours(8);
        _tokens.EmitirAsync(usuario, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(("tok_abc_xyz", expires)));

        var handler = NovoHandler();
        var resp = await handler.HandleAsync(new LoginCommand(usuario.EmailValor, "Senha1234"), CancellationToken.None);

        resp.AccessToken.Should().Be("tok_abc_xyz");
        resp.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        resp.Usuario.Id.Should().Be(usuario.Id);
        resp.Usuario.Nome.Should().Be(usuario.Nome);
        resp.Usuario.Email.Should().Be(usuario.EmailValor);
        resp.Usuario.Perfil.Should().Be(usuario.Perfil);

        await _auditoria.Received(1).LogAsync(
            LoginHandler.EventoSucesso,
            LoginHandler.EntidadeAuditoria,
            usuario.Id,
            Arg.Any<object?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Quando_usuario_nao_existe_Verify_e_chamado_com_DummyPasswordHash_Value()
    {
        _repo.ObterPorEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Usuario?)null);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(new LoginCommand("nada@aqui.local", "qualquer123"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
        _hasher.Received(1).Verify("qualquer123", DummyHash);
    }

    [Fact]
    public async Task NeedsRehash_true_chama_TrocarSenha_e_SalvarAsync()
    {
        var usuario = NovoUsuario(ativo: true);
        _repo.ObterPorEmailAsync(usuario.EmailValor, Arg.Any<CancellationToken>()).Returns(usuario);
        _hasher.Verify("Senha1234", usuario.SenhaHash).Returns(true);
        _hasher.NeedsRehash(usuario.SenhaHash).Returns(true);
        _hasher.Hash("Senha1234").Returns("$argon2id$v=19$m=131072,t=3,p=1$bm92bw$bm92b2hhc2g");
        _tokens.EmitirAsync(usuario, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(("tok", DateTime.UtcNow.AddHours(8))));

        var handler = NovoHandler();
        var resp = await handler.HandleAsync(new LoginCommand(usuario.EmailValor, "Senha1234"), CancellationToken.None);

        resp.AccessToken.Should().Be("tok");
        usuario.SenhaHash.Should().Be("$argon2id$v=19$m=131072,t=3,p=1$bm92bw$bm92b2hhc2g");
        _hasher.Received(1).Hash("Senha1234");
        await _repo.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Email_uppercase_eh_normalizado_para_lowercase_antes_de_buscar()
    {
        var usuario = NovoUsuario(ativo: true);
        _repo.ObterPorEmailAsync(usuario.EmailValor, Arg.Any<CancellationToken>()).Returns(usuario);
        _hasher.Verify("Senha1234", usuario.SenhaHash).Returns(true);
        _hasher.NeedsRehash(usuario.SenhaHash).Returns(false);
        _tokens.EmitirAsync(usuario, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(("tok", DateTime.UtcNow.AddHours(8))));

        var handler = NovoHandler();
        var resp = await handler.HandleAsync(new LoginCommand("  ALICE@CARWASH.LOCAL  ", "Senha1234"), CancellationToken.None);

        resp.Usuario.Email.Should().Be("alice@carwash.local");
        await _repo.Received(1).ObterPorEmailAsync("alice@carwash.local", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Login_com_usuario_bloqueado_lanca_UsuarioBloqueado_sem_incrementar_contador()
    {
        var usuario = NovoUsuario(ativo: true);
        BloquearUsuario(usuario, minutosNoFuturo: 5, tentativasIniciais: 3);
        _repo.ObterPorEmailAsync(usuario.EmailValor, Arg.Any<CancellationToken>()).Returns(usuario);
        _hasher.Verify("Senha1234", usuario.SenhaHash).Returns(true);

        var handler = NovoHandler();
        var act = () => handler.HandleAsync(new LoginCommand(usuario.EmailValor, "Senha1234"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<UsuarioBloqueadoException>();
        ex.Which.BloqueadoAte.Should().BeAfter(DateTime.UtcNow);

        // Bloqueio ativo NÃO incrementa nem persiste.
        usuario.TentativasInvalidas.Should().Be(3);
        await _repo.DidNotReceive().SalvarAsync(Arg.Any<CancellationToken>());

        await _auditoria.Received(1).LogAsync(
            LoginHandler.EventoFalha,
            LoginHandler.EntidadeAuditoria,
            usuario.Id,
            Arg.Is<object>(o => DadosTem(o, "Motivo", LoginHandler.MotivoUsuarioBloqueado)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Tres_falhas_consecutivas_bloqueiam_usuario_e_lancam_403_na_terceira()
    {
        var usuario = NovoUsuario(ativo: true);
        _repo.ObterPorEmailAsync(usuario.EmailValor, Arg.Any<CancellationToken>()).Returns(usuario);
        _hasher.Verify("ErradaXYZ", usuario.SenhaHash).Returns(false);

        var handler = NovoHandler();

        // 1ª falha — 401
        await handler.Invoking(h => h.HandleAsync(new LoginCommand(usuario.EmailValor, "ErradaXYZ"), CancellationToken.None))
            .Should().ThrowAsync<InvalidCredentialsException>();
        usuario.TentativasInvalidas.Should().Be(1);
        usuario.BloqueadoAte.Should().BeNull();

        // 2ª falha — 401
        await handler.Invoking(h => h.HandleAsync(new LoginCommand(usuario.EmailValor, "ErradaXYZ"), CancellationToken.None))
            .Should().ThrowAsync<InvalidCredentialsException>();
        usuario.TentativasInvalidas.Should().Be(2);
        usuario.BloqueadoAte.Should().BeNull();

        // 3ª falha — vira bloqueio (403)
        var act = () => handler.HandleAsync(new LoginCommand(usuario.EmailValor, "ErradaXYZ"), CancellationToken.None);
        var ex = await act.Should().ThrowAsync<UsuarioBloqueadoException>();
        ex.Which.BloqueadoAte.Should().BeCloseTo(DateTime.UtcNow.Add(LoginHandler.DuracaoBloqueio), TimeSpan.FromSeconds(5));

        usuario.TentativasInvalidas.Should().Be(LoginHandler.LimiteTentativasInvalidas);
        usuario.BloqueadoAte.Should().NotBeNull();

        // 3 SalvarAsync (uma por falha).
        await _repo.Received(3).SalvarAsync(Arg.Any<CancellationToken>());

        // O evento UsuarioContaBloqueada é auditado exatamente uma vez (na 3ª falha).
        await _auditoria.Received(1).LogAsync(
            LoginHandler.EventoUsuarioBloqueado,
            LoginHandler.EntidadeAuditoria,
            usuario.Id,
            Arg.Any<object?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Sucesso_apos_falhas_zera_contador_e_libera_bloqueio()
    {
        var usuario = NovoUsuario(ativo: true);

        // Simula histórico: 2 falhas anteriores acumuladas (ainda não bloqueado).
        usuario.RegistrarFalhaDeLogin(DateTime.UtcNow, LoginHandler.LimiteTentativasInvalidas, LoginHandler.DuracaoBloqueio);
        usuario.RegistrarFalhaDeLogin(DateTime.UtcNow, LoginHandler.LimiteTentativasInvalidas, LoginHandler.DuracaoBloqueio);
        usuario.TentativasInvalidas.Should().Be(2);

        _repo.ObterPorEmailAsync(usuario.EmailValor, Arg.Any<CancellationToken>()).Returns(usuario);
        _hasher.Verify("Senha1234", usuario.SenhaHash).Returns(true);
        _hasher.NeedsRehash(usuario.SenhaHash).Returns(false);
        _tokens.EmitirAsync(usuario, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(("tok", DateTime.UtcNow.AddHours(8))));

        var handler = NovoHandler();
        var resp = await handler.HandleAsync(new LoginCommand(usuario.EmailValor, "Senha1234"), CancellationToken.None);

        resp.AccessToken.Should().Be("tok");
        usuario.TentativasInvalidas.Should().Be(0);
        usuario.BloqueadoAte.Should().BeNull();

        // O sucesso deve persistir o reset (mesmo sem rehash).
        await _repo.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Bloqueio_expirado_permite_nova_tentativa_e_zera_contador_no_sucesso()
    {
        var usuario = NovoUsuario(ativo: true);

        // Bloqueio que já expirou.
        BloquearUsuario(usuario, minutosNoFuturo: -1, tentativasIniciais: 3);
        usuario.EstaBloqueado(DateTime.UtcNow).Should().BeFalse();

        _repo.ObterPorEmailAsync(usuario.EmailValor, Arg.Any<CancellationToken>()).Returns(usuario);
        _hasher.Verify("Senha1234", usuario.SenhaHash).Returns(true);
        _hasher.NeedsRehash(usuario.SenhaHash).Returns(false);
        _tokens.EmitirAsync(usuario, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(("tok", DateTime.UtcNow.AddHours(8))));

        var handler = NovoHandler();
        var resp = await handler.HandleAsync(new LoginCommand(usuario.EmailValor, "Senha1234"), CancellationToken.None);

        resp.AccessToken.Should().Be("tok");
        usuario.TentativasInvalidas.Should().Be(0);
        usuario.BloqueadoAte.Should().BeNull();
    }

    private LoginHandler NovoHandler() =>
        new(_repo, _hasher, _tokens, _auditoria, _contexto, _dummy, NullLogger<LoginHandler>.Instance);

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

    private static bool DadosTem(object dados, string nome, object esperado)
    {
        var prop = dados.GetType().GetProperty(nome);
        if (prop is null)
        {
            return false;
        }

        var valor = prop.GetValue(dados);
        return Equals(valor, esperado);
    }

    /// <summary>
    /// Helper de teste: força o estado de bloqueio do usuário sem depender de N chamadas
    /// a <c>RegistrarFalhaDeLogin</c>. Usa reflection para escrever nos setters privados.
    /// </summary>
    private static void BloquearUsuario(Usuario usuario, double minutosNoFuturo, int tentativasIniciais)
    {
        var tipo = typeof(Usuario);
        tipo.GetProperty(nameof(Usuario.TentativasInvalidas))!
            .SetValue(usuario, tentativasIniciais);
        tipo.GetProperty(nameof(Usuario.BloqueadoAte))!
            .SetValue(usuario, DateTime.UtcNow.AddMinutes(minutosNoFuturo));
    }
}
