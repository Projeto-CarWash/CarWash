using CarWash.Application.Filiais.AlterarCelulasAtivas;
using FluentValidation.TestHelper;
using Xunit;

namespace CarWash.UnitTests.Application.Filiais;

/// <summary>
/// RF018 — valida o command de alteração de células ativas. Mensagem da faixa
/// EXATA conforme o card.
/// </summary>
public class AlterarCelulasAtivasCommandValidatorTests
{
    private readonly AlterarCelulasAtivasCommandValidator _validator = new();

    [Fact]
    public void Id_vazio_falha()
    {
        var cmd = new AlterarCelulasAtivasCommand(Guid.Empty, 4);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.FilialId)
              .WithErrorMessage(AlterarCelulasAtivasCommandValidator.MensagemFilialIdInvalido);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    public void Faixa_valida_passa(int celulas)
    {
        var cmd = new AlterarCelulasAtivasCommand(Guid.NewGuid(), celulas);
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.CelulasAtivas);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(999)]
    public void Faixa_invalida_falha_com_mensagem_exata(int celulas)
    {
        var cmd = new AlterarCelulasAtivasCommand(Guid.NewGuid(), celulas);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.CelulasAtivas)
              .WithErrorMessage(AlterarCelulasAtivasCommandValidator.MensagemFaixa);
    }

    [Fact]
    public void CelulasAtivas_null_falha_como_obrigatorio()
    {
        var cmd = new AlterarCelulasAtivasCommand(Guid.NewGuid(), null);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.CelulasAtivas)
              .WithErrorMessage(AlterarCelulasAtivasCommandValidator.MensagemCelulasObrigatorio);
    }

    [Fact]
    public void Payload_valido_completo_nao_tem_erros()
    {
        var cmd = new AlterarCelulasAtivasCommand(Guid.NewGuid(), 7);
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
