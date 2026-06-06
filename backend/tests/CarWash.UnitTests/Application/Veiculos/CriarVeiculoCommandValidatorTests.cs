using CarWash.Application.Veiculos.Criar;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace CarWash.UnitTests.Application.Veiculos;

public class CriarVeiculoCommandValidatorTests
{
    private readonly CriarVeiculoCommandValidator _sut = new();

    [Fact]
    public async Task Placa_nula_retorna_erro_obrigatoria()
    {
        var command = ComandoValido() with { Placa = null };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Placa)
            .WithErrorMessage("O campo placa é obrigatório.");
    }

    [Fact]
    public async Task Placa_vazia_retorna_erro_obrigatoria()
    {
        var command = ComandoValido() with { Placa = string.Empty };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Placa)
            .WithErrorMessage("O campo placa é obrigatório.");
    }

    [Fact]
    public async Task Placa_menos_de_7_caracteres_retorna_erro_formato()
    {
        var command = ComandoValido() with { Placa = "ABC12" };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Placa)
            .WithErrorMessage("A placa informada não está em um formato válido.");
    }

    [Fact]
    public async Task Placa_mais_de_7_caracteres_retorna_erro_formato()
    {
        var command = ComandoValido() with { Placa = "ABC12345" };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Placa)
            .WithErrorMessage("A placa informada não está em um formato válido.");
    }

    [Fact]
    public async Task Placa_com_caractere_especial_retorna_erro_formato()
    {
        var command = ComandoValido() with { Placa = "ABC-123" };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Placa)
            .WithErrorMessage("A placa informada não está em um formato válido.");
    }

    [Fact]
    public async Task Placa_somente_numeros_retorna_erro_formato()
    {
        var command = ComandoValido() with { Placa = "1234567" };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Placa)
            .WithErrorMessage("A placa informada não está em um formato válido.");
    }

    [Fact]
    public async Task Placa_somente_letras_retorna_erro_formato()
    {
        var command = ComandoValido() with { Placa = "ABCDEFG" };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Placa)
            .WithErrorMessage("A placa informada não está em um formato válido.");
    }

    [Fact]
    public async Task Placa_padrao_antigo_valida_com_sucesso()
    {
        var command = ComandoValido() with { Placa = "ABC1234" };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Placa);
    }

    [Fact]
    public async Task Placa_padrao_mercosul_valida_com_sucesso()
    {
        var command = ComandoValido() with { Placa = "ABC1D23" };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Placa);
    }

    [Fact]
    public async Task Placa_minuscula_normaliza_e_valida_com_sucesso()
    {
        var command = ComandoValido() with { Placa = "abc1234" };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Placa);
    }

    private static CriarVeiculoCommand ComandoValido() => new(
        ClienteId: Guid.NewGuid(),
        Placa: "ABC1D23",
        Modelo: "Onix",
        Fabricante: "Chevrolet",
        Cor: "Prata",
        Ano: 2022,
        TraceId: "trace-1",
        UsuarioId: null);
}
