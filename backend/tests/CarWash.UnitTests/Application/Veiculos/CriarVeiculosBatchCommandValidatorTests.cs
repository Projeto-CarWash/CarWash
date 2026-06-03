using CarWash.Application.Veiculos.CriarBatch;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace CarWash.UnitTests.Application.Veiculos;

public class CriarVeiculosBatchCommandValidatorTests
{
    private readonly CriarVeiculosBatchCommandValidator _sut = new();

    [Fact]
    public async Task Payload_com_placas_duplicadas_retorna_erro()
    {
        var command = ComandoBatchValido();
        command = command with
        {
            Veiculos = new List<VeiculoItemCommand>
            {
                new("ABC1D23", "Onix", "Chevrolet", "Prata", 2022),
                new("ABC1D23", "Corolla", "Toyota", "Preto", 2023),
            }
        };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Veiculos)
            .WithErrorMessage("O payload contém placas duplicadas.");
    }

    [Fact]
    public async Task Payload_com_placas_distintas_valida_com_sucesso()
    {
        var command = ComandoBatchValido();

        var result = await _sut.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Veiculos);
    }

    [Fact]
    public async Task Item_com_placa_nula_retorna_erro_obrigatoria()
    {
        var command = ComandoBatchValido();
        command = command with
        {
            Veiculos = new List<VeiculoItemCommand>
            {
                new(null, "Onix", "Chevrolet", "Prata", 2022),
            }
        };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor("Veiculos[0].Placa")
            .WithErrorMessage("O campo placa é obrigatório.");
    }

    [Fact]
    public async Task Item_com_placa_com_caractere_especial_retorna_erro_formato()
    {
        var command = ComandoBatchValido();
        command = command with
        {
            Veiculos = new List<VeiculoItemCommand>
            {
                new("ABC-123", "Onix", "Chevrolet", "Prata", 2022),
            }
        };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor("Veiculos[0].Placa")
            .WithErrorMessage("A placa informada não está em um formato válido.");
    }

    [Fact]
    public async Task Item_com_placa_somente_numeros_retorna_erro_formato()
    {
        var command = ComandoBatchValido();
        command = command with
        {
            Veiculos = new List<VeiculoItemCommand>
            {
                new("1234567", "Onix", "Chevrolet", "Prata", 2022),
            }
        };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor("Veiculos[0].Placa")
            .WithErrorMessage("A placa informada não está em um formato válido.");
    }

    [Fact]
    public async Task Item_com_placa_somente_letras_retorna_erro_formato()
    {
        var command = ComandoBatchValido();
        command = command with
        {
            Veiculos = new List<VeiculoItemCommand>
            {
                new("ABCDEFG", "Onix", "Chevrolet", "Prata", 2022),
            }
        };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor("Veiculos[0].Placa")
            .WithErrorMessage("A placa informada não está em um formato válido.");
    }

    private static CriarVeiculosBatchCommand ComandoBatchValido() => new(
        ClienteId: Guid.NewGuid(),
        Veiculos: new List<VeiculoItemCommand>
        {
            new("ABC1D23", "Onix", "Chevrolet", "Prata", 2022),
            new("XYZ1234", "Corolla", "Toyota", "Preto", 2023),
        },
        TraceId: "trace-batch-1",
        UsuarioId: null);
}
