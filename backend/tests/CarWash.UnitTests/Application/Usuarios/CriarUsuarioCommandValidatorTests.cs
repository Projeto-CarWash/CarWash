using CarWash.Application.Usuarios.CriarUsuario;
using CarWash.Domain.Enums;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace CarWash.UnitTests.Application.Usuarios;

public class CriarUsuarioCommandValidatorTests
{
    private readonly CriarUsuarioCommandValidator _validator = new();

    [Fact]
    public void Payload_valido_passa()
    {
        var cmd = NovoComando();
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Nome_vazio_falha()
    {
        var cmd = NovoComando() with { Nome = "  " };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Nome);
    }

    [Theory]
    [InlineData("naoeemail")]
    [InlineData("sem@dominio")]
    [InlineData("@dominio.com")]
    [InlineData("espaco @local.com")]
    public void Email_malformado_falha(string email)
    {
        var cmd = NovoComando() with { Email = email };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Senha_curta_falha_com_mensagem_de_politica()
    {
        var cmd = NovoComando() with { Senha = "abc12" };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Senha)
              .WithErrorMessage(CriarUsuarioCommandValidator.MensagemSenhaFraca);
    }

    [Fact]
    public void Senha_sem_letra_falha()
    {
        var cmd = NovoComando() with { Senha = "12345678" };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Senha)
              .WithErrorMessage(CriarUsuarioCommandValidator.MensagemSenhaFraca);
    }

    [Fact]
    public void Senha_sem_numero_falha()
    {
        var cmd = NovoComando() with { Senha = "abcdefgh" };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Senha)
              .WithErrorMessage(CriarUsuarioCommandValidator.MensagemSenhaFraca);
    }

    [Fact]
    public void Perfil_invalido_falha()
    {
        var cmd = NovoComando() with { Perfil = (PerfilUsuario)99 };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Perfil);
    }

    [Fact]
    public void Senha_muito_longa_falha()
    {
        var cmd = NovoComando() with { Senha = new string('a', 129) + "1" };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Senha);
    }

    [Fact]
    public void Mensagem_de_payload_invalido_disponivel_como_constante()
    {
        CriarUsuarioCommandValidator.MensagemPayloadInvalido
            .Should().Be("Dados do usuário inválidos. Verifique os campos e tente novamente.");
    }

    private static CriarUsuarioCommand NovoComando() => new(
        Nome: "Alice Silva",
        Email: "alice@carwash.local",
        Senha: "Senha1234",
        Perfil: PerfilUsuario.Funcionario);
}
