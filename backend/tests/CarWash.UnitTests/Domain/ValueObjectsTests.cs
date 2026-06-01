using CarWash.Domain.Common;
using CarWash.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace CarWash.UnitTests.Domain;

public class ValueObjectsTests
{
    [Theory]
    [InlineData(" ADM@CarWash.local ", "adm@carwash.local")]
    [InlineData("teste@dominio.com.br", "teste@dominio.com.br")]
    public void Email_normaliza_para_lowercase_e_trim(string entrada, string esperado)
    {
        var email = new Email(entrada);
        email.Valor.Should().Be(esperado);
    }

    [Theory]
    [InlineData("invalido")]
    [InlineData("a@")]
    [InlineData("@b")]
    [InlineData("")]
    public void Email_invalido_lanca_DomainException(string entrada)
    {
        var act = () => new Email(entrada);
        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(" ABC1D23 ", "ABC1D23")]
    [InlineData("abc1d23", "ABC1D23")]
    [InlineData("ABC1234", "ABC1234")]
    [InlineData("ABC1D23", "ABC1D23")]
    public void Placa_normaliza_para_uppercase_com_trim(string entrada, string esperado)
    {
        var placa = new Placa(entrada);
        placa.Valor.Should().Be(esperado);
    }

    [Theory]
    [InlineData("ABC-123", "A placa informada não está em um formato válido.")]
    [InlineData("AB@1234", "A placa informada não está em um formato válido.")]
    [InlineData("1234567", "A placa informada não está em um formato válido.")]
    [InlineData("ABCDEFG", "A placa informada não está em um formato válido.")]
    [InlineData("AB12345", "A placa informada não está em um formato válido.")]
    [InlineData("", "O campo placa é obrigatório.")]
    public void Placa_invalida_lanca_DomainException_com_mensagem_padronizada(string entrada, string mensagemEsperada)
    {
        var act = () => new Placa(entrada);
        act.Should().Throw<DomainException>().WithMessage(mensagemEsperada);
    }

    [Fact]
    public void Placa_menos_de_7_caracteres_lanca_DomainException()
    {
        var act = () => new Placa("ABC12");
        act.Should().Throw<DomainException>().WithMessage("A placa informada não está em um formato válido.");
    }

    [Fact]
    public void Placa_mais_de_7_caracteres_lanca_DomainException()
    {
        var act = () => new Placa("ABC12345");
        act.Should().Throw<DomainException>().WithMessage("A placa informada não está em um formato válido.");
    }

    [Fact]
    public void Placa_com_caractere_especial_lanca_DomainException()
    {
        var act = () => new Placa("ABC-123");
        act.Should().Throw<DomainException>().WithMessage("A placa informada não está em um formato válido.");
    }

    [Fact]
    public void Placa_somente_numeros_lanca_DomainException()
    {
        var act = () => new Placa("1234567");
        act.Should().Throw<DomainException>().WithMessage("A placa informada não está em um formato válido.");
    }

    [Fact]
    public void Placa_somente_letras_lanca_DomainException()
    {
        var act = () => new Placa("ABCDEFG");
        act.Should().Throw<DomainException>().WithMessage("A placa informada não está em um formato válido.");
    }

    [Fact]
    public void Placa_nula_lanca_DomainException()
    {
        var act = () => new Placa(null!);
        act.Should().Throw<DomainException>().WithMessage("O campo placa é obrigatório.");
    }

    [Fact]
    public void Placa_vazia_lanca_DomainException()
    {
        var act = () => new Placa("");
        act.Should().Throw<DomainException>().WithMessage("O campo placa é obrigatório.");
    }

    [Theory]
    [InlineData("390.533.447-05", "39053344705")]
    [InlineData("39053344705", "39053344705")]
    public void Cpf_normaliza_para_digitos(string entrada, string esperado)
    {
        var cpf = new Cpf(entrada);
        cpf.Valor.Should().Be(esperado);
    }

    [Theory]
    [InlineData("11111111111")]
    [InlineData("12345678900")]
    [InlineData("12345")]
    public void Cpf_invalido_lanca_DomainException(string entrada)
    {
        var act = () => new Cpf(entrada);
        act.Should().Throw<DomainException>();
    }
}
