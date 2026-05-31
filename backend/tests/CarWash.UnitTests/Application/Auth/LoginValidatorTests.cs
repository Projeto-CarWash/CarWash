using CarWash.Application.Auth.Login;
using FluentValidation.TestHelper;
using Xunit;

namespace CarWash.UnitTests.Application.Auth;

public class LoginValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Payload_valido_passa()
    {
        var result = _validator.TestValidate(new LoginCommand("alice@carwash.local", "Senha1234"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Email_vazio_falha()
    {
        var result = _validator.TestValidate(new LoginCommand(string.Empty, "Senha1234"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Senha_vazia_falha()
    {
        var result = _validator.TestValidate(new LoginCommand("alice@carwash.local", string.Empty));
        result.ShouldHaveValidationErrorFor(x => x.Senha);
    }

    [Theory]
    [InlineData("naoeemail")]
    [InlineData("sem@dominio")]
    [InlineData("@dominio.com")]
    public void Email_malformado_passa_no_validator(string email)
    {
        // Decisão: o validator NÃO checa formato. Handler trata como 401 para evitar
        // oráculo de enumeração via 400 do validator.
        var result = _validator.TestValidate(new LoginCommand(email, "Senha1234"));
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Email_muito_longo_falha()
    {
        string email = new string('a', 145) + "@x.com"; // > 150
        var result = _validator.TestValidate(new LoginCommand(email, "Senha1234"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Senha_muito_longa_falha()
    {
        string senha = new string('a', 257);
        var result = _validator.TestValidate(new LoginCommand("alice@carwash.local", senha));
        result.ShouldHaveValidationErrorFor(x => x.Senha);
    }
}
