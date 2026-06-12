using CarWash.Application.Responsaveis.AlterarStatus;
using FluentAssertions;
using Xunit;

namespace CarWash.UnitTests.Application.Responsaveis;

public class AlterarStatusResponsavelCommandValidatorTests
{
    private readonly AlterarStatusResponsavelCommandValidator _validator = new();

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Comando_valido_passa(bool ativo)
    {
        var resultado = _validator.Validate(ComandoValido() with { Ativo = ativo });
        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Ativo_ausente_falha()
    {
        var resultado = _validator.Validate(ComandoValido() with { Ativo = null });
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e =>
            e.ErrorMessage == AlterarStatusResponsavelCommandValidator.MensagemAtivoObrigatorio);
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

    private static AlterarStatusResponsavelCommand ComandoValido() => new(
        ResponsavelId: Guid.NewGuid(),
        ClienteTitularId: Guid.NewGuid(),
        Ativo: true,
        TraceId: "trace-1",
        UsuarioId: Guid.NewGuid());
}
