using CarWash.Application.Responsaveis.Criar;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace CarWash.UnitTests.Application.Responsaveis;

public class CriarResponsavelCommandValidatorTests
{
    private readonly CriarResponsavelCommandValidator _sut = new();

    [Fact]
    public async Task Comando_valido_nao_retorna_erros()
    {
        var command = ComandoValido();

        var result = await _sut.TestValidateAsync(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ClienteTitularId_vazio_retorna_erro()
    {
        var command = ComandoValido() with { ClienteTitularId = Guid.Empty };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.ClienteTitularId)
            .WithErrorMessage("Identificador do cliente titular é obrigatório.");
    }

    [Fact]
    public async Task Nome_nulo_retorna_erro_obrigatorio()
    {
        var command = ComandoValido() with { Nome = null };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Nome)
            .WithErrorMessage("O nome é obrigatório.");
    }

    [Fact]
    public async Task Nome_vazio_retorna_erro_obrigatorio()
    {
        var command = ComandoValido() with { Nome = "" };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Nome);
    }

    [Fact]
    public async Task Nome_com_menos_de_3_caracteres_retorna_erro()
    {
        var command = ComandoValido() with { Nome = "Ab" };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Nome)
            .WithErrorMessage("O nome deve ter no mínimo 3 caracteres.");
    }

    [Fact]
    public async Task Nome_com_mais_de_100_caracteres_retorna_erro()
    {
        var command = ComandoValido() with { Nome = new string('A', 101) };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Nome)
            .WithErrorMessage("O nome deve ter no máximo 100 caracteres.");
    }

    [Fact]
    public async Task Nome_com_exatamente_3_caracteres_valida_com_sucesso()
    {
        var command = ComandoValido() with { Nome = "Ana" };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Nome);
    }

    [Fact]
    public async Task Documento_nulo_retorna_erro_obrigatorio()
    {
        var command = ComandoValido() with { Documento = null };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Documento)
            .WithErrorMessage("O documento é obrigatório.");
    }

    [Fact]
    public async Task Documento_com_letras_retorna_erro_apenas_numeros()
    {
        var command = ComandoValido() with { Documento = "3905334470a" };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Documento)
            .WithErrorMessage("Documento deve conter apenas números.");
    }

    [Fact]
    public async Task Documento_cpf_invalido_retorna_erro_documento_invalido()
    {
        var command = ComandoValido() with { Documento = "11111111111" };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Documento)
            .WithErrorMessage("Documento inválido.");
    }

    [Fact]
    public async Task Documento_cnpj_invalido_retorna_erro_documento_invalido()
    {
        var command = ComandoValido() with { Documento = "11111111111111" };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Documento)
            .WithErrorMessage("Documento inválido.");
    }

    [Fact]
    public async Task Documento_com_tamanho_diferente_de_11_e_14_retorna_erro()
    {
        var command = ComandoValido() with { Documento = "1234567890" };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Documento)
            .WithErrorMessage("Documento inválido.");
    }

    [Fact]
    public async Task Documento_cpf_valido_nao_retorna_erro()
    {
        var command = ComandoValido() with { Documento = "39053344705" };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Documento);
    }

    [Fact]
    public async Task Telefone_com_letras_retorna_erro()
    {
        var command = ComandoValido() with { Telefone = "1198765432a" };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Telefone)
            .WithErrorMessage("Telefone deve conter apenas números.");
    }

    [Fact]
    public async Task Telefone_com_menos_de_10_digitos_retorna_erro()
    {
        var command = ComandoValido() with { Telefone = "119876543" };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Telefone)
            .WithErrorMessage("Telefone deve conter entre 10 e 11 dígitos.");
    }

    [Fact]
    public async Task Telefone_com_mais_de_11_digitos_retorna_erro()
    {
        var command = ComandoValido() with { Telefone = "119876543212" };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Telefone)
            .WithErrorMessage("Telefone deve conter entre 10 e 11 dígitos.");
    }

    [Fact]
    public async Task Telefone_nulo_nao_retorna_erro()
    {
        var command = ComandoValido() with { Telefone = null };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Telefone);
    }

    [Fact]
    public async Task Email_com_menos_de_5_caracteres_retorna_erro()
    {
        var command = ComandoValido() with { Email = "a@b" };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("E-mail deve ter no mínimo 5 caracteres.");
    }

    [Fact]
    public async Task Email_com_mais_de_150_caracteres_retorna_erro()
    {
        var command = ComandoValido() with { Email = $"{new string('a', 147)}@x.com" };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("E-mail deve ter no máximo 150 caracteres.");
    }

    [Fact]
    public async Task Email_invalido_retorna_erro()
    {
        var command = ComandoValido() with { Email = "nao-e-email" };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("E-mail inválido.");
    }

    [Fact]
    public async Task Email_nulo_nao_retorna_erro()
    {
        var command = ComandoValido() with { Email = null };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public async Task GrauVinculo_nulo_retorna_erro_obrigatorio()
    {
        var command = ComandoValido() with { GrauVinculo = null };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.GrauVinculo)
            .WithErrorMessage("Grau de vínculo é obrigatório.");
    }

    [Fact]
    public async Task GrauVinculo_invalido_retorna_erro()
    {
        var command = ComandoValido() with { GrauVinculo = "INVALIDO" };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.GrauVinculo);
    }

    [Theory]
    [InlineData("RESPONSAVEL_FINANCEIRO")]
    [InlineData("RESPONSAVEL_LEGAL")]
    [InlineData("PROCURADOR")]
    [InlineData("CONJUGE")]
    [InlineData("PAI_MAE")]
    [InlineData("OUTRO")]
    public async Task GrauVinculo_valido_nao_retorna_erro(string grau)
    {
        var command = ComandoValido() with { GrauVinculo = grau };

        var result = await _sut.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(x => x.GrauVinculo);
    }

    private static CriarResponsavelCommand ComandoValido() => new(
        ClienteTitularId: Guid.NewGuid(),
        Nome: "João Silva",
        Documento: "39053344705",
        Telefone: "11987654321",
        Email: "joao@email.com",
        GrauVinculo: "RESPONSAVEL_FINANCEIRO",
        TraceId: "trace-1",
        UsuarioId: null);
}
