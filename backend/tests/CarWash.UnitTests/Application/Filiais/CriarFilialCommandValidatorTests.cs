using CarWash.Application.Filiais.CriarFilial;
using FluentValidation.TestHelper;
using Xunit;

namespace CarWash.UnitTests.Application.Filiais;

/// <summary>
/// RF018 — valida o command de criação de filial. A mensagem da faixa é EXATA
/// conforme o card; o front e os testes E2E dependem dessa string.
/// </summary>
public class CriarFilialCommandValidatorTests
{
    private readonly CriarFilialCommandValidator _validator = new();

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    public void Faixa_valida_passa(int celulas)
    {
        var cmd = NovoComando() with { CelulasAtivas = celulas };
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
        var cmd = NovoComando() with { CelulasAtivas = celulas };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.CelulasAtivas)
              .WithErrorMessage(CriarFilialCommandValidator.MensagemFaixa);
    }

    [Fact]
    public void CelulasAtivas_null_falha_como_obrigatorio()
    {
        var cmd = NovoComando() with { CelulasAtivas = null };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.CelulasAtivas)
              .WithErrorMessage(CriarFilialCommandValidator.MensagemCelulasObrigatorio);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Nome_vazio_ou_null_falha(string? nome)
    {
        var cmd = NovoComando() with { Nome = nome };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Nome)
              .WithErrorMessage(CriarFilialCommandValidator.MensagemNomeObrigatorio);
    }

    [Fact]
    public void Nome_no_limite_de_120_passa()
    {
        var cmd = NovoComando() with { Nome = new string('a', 120) };
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.Nome);
    }

    [Fact]
    public void Nome_acima_de_120_caracteres_falha()
    {
        var cmd = NovoComando() with { Nome = new string('a', 121) };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Nome)
              .WithErrorMessage(CriarFilialCommandValidator.MensagemNomeMaximo);
    }

    [Fact]
    public void Timezone_acima_de_64_caracteres_falha()
    {
        var cmd = NovoComando() with { Timezone = new string('z', 65) };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Timezone)
              .WithErrorMessage(CriarFilialCommandValidator.MensagemTimezoneMaximo);
    }

    [Fact]
    public void Timezone_null_passa_pois_e_opcional()
    {
        var cmd = NovoComando() with { Timezone = null };
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.Timezone);
    }

    [Fact]
    public void Payload_valido_completo_nao_tem_erros()
    {
        var result = _validator.TestValidate(NovoComando());
        result.ShouldNotHaveAnyValidationErrors();
    }

    private static CriarFilialCommand NovoComando() => new(
        Nome: "Filial Centro",
        CelulasAtivas: 4,
        Timezone: "America/Sao_Paulo");
}
