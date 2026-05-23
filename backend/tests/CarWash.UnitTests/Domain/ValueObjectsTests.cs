using CarWash.Domain.Common;
using CarWash.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace CarWash.UnitTests.Domain;

public class ValueObjectsTests
{
    [Theory]
    [InlineData("  ADM@CarWash.local  ", "adm@carwash.local")]
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
    [InlineData("abc-1234", "ABC1234")]
    [InlineData(" ABC1D23 ", "ABC1D23")]
    public void Placa_normaliza_para_uppercase_sem_espacos(string entrada, string esperado)
    {
        var placa = new Placa(entrada);
        placa.Valor.Should().Be(esperado);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("AB12345")]
    public void Placa_invalida_lanca_DomainException(string entrada)
    {
        var act = () => new Placa(entrada);
        act.Should().Throw<DomainException>();
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
