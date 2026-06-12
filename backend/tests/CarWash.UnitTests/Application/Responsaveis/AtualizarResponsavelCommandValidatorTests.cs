using CarWash.Application.Responsaveis.Atualizar;
using FluentAssertions;
using Xunit;

namespace CarWash.UnitTests.Application.Responsaveis;

public class AtualizarResponsavelCommandValidatorTests
{
    private readonly AtualizarResponsavelCommandValidator _validator = new();

    [Fact]
    public void Comando_valido_passa()
    {
        var resultado = _validator.Validate(ComandoValido());
        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ResponsavelId_vazio_falha()
    {
        var resultado = _validator.Validate(ComandoValido() with { ResponsavelId = Guid.Empty });
        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ClienteTitularId_vazio_falha()
    {
        var resultado = _validator.Validate(ComandoValido() with { ClienteTitularId = Guid.Empty });
        resultado.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ab")]
    public void Nome_invalido_falha(string? nome)
    {
        var resultado = _validator.Validate(ComandoValido() with { Nome = nome });
        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Nome_acima_de_100_caracteres_falha()
    {
        var resultado = _validator.Validate(ComandoValido() with { Nome = new string('a', 101) });
        resultado.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData("119876543210000")]
    public void Telefone_invalido_falha(string telefone)
    {
        var resultado = _validator.Validate(ComandoValido() with { Telefone = telefone });
        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Telefone_ausente_passa()
    {
        var resultado = _validator.Validate(ComandoValido() with { Telefone = null });
        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Email_invalido_falha()
    {
        var resultado = _validator.Validate(ComandoValido() with { Email = "nao-eh-email" });
        resultado.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("PRIMO")]
    public void GrauVinculo_invalido_falha(string? grau)
    {
        var resultado = _validator.Validate(ComandoValido() with { GrauVinculo = grau });
        resultado.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("RESPONSAVEL_FINANCEIRO")]
    [InlineData("RESPONSAVEL_LEGAL")]
    [InlineData("PROCURADOR")]
    [InlineData("CONJUGE")]
    [InlineData("PAI_MAE")]
    [InlineData("OUTRO")]
    public void GrauVinculo_valido_passa(string grau)
    {
        var resultado = _validator.Validate(ComandoValido() with { GrauVinculo = grau });
        resultado.IsValid.Should().BeTrue();
    }

    private static AtualizarResponsavelCommand ComandoValido() => new(
        ResponsavelId: Guid.NewGuid(),
        ClienteTitularId: Guid.NewGuid(),
        Nome: "Maria Silva",
        Telefone: "11987654321",
        Email: "maria@x.com",
        GrauVinculo: "RESPONSAVEL_FINANCEIRO",
        CamposExtras: null,
        TraceId: "trace-1",
        UsuarioId: Guid.NewGuid());
}
