using CarWash.Application.Filiais.Common;
using CarWash.Application.Filiais.Criar;
using FluentValidation.TestHelper;
using Xunit;

namespace CarWash.UnitTests.Application.Filiais;

/// <summary>
/// Cobertura do validator RF017 + RF018 — ADR-0007 §8.1 (BE-14). Mínimo:
/// nome (3 casos), código (2 casos), CNPJ inválido, UF inválida, células
/// fora da faixa, payload válido.
/// </summary>
public class CriarFilialCommandValidatorTests
{
    private readonly CriarFilialCommandValidator _validator = new();

    [Fact]
    public void Payload_valido_passa()
    {
        var cmd = NovoComando();
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Nome_ausente_falha(string? nome)
    {
        var cmd = NovoComando() with { Nome = nome };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Nome);
    }

    [Fact]
    public void Nome_curto_falha()
    {
        var cmd = NovoComando() with { Nome = "ab" };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Nome);
    }

    [Fact]
    public void Nome_longo_falha()
    {
        var cmd = NovoComando() with { Nome = new string('A', 121) };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Nome);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("A")] // < 2
    [InlineData("MTZ-01")] // hífen
    [InlineData("MTZ 01")] // espaço
    [InlineData("ABCDEFGHIJKLMNOPQRSTU")] // > 20
    public void Codigo_invalido_falha(string? codigo)
    {
        var cmd = NovoComando() with { Codigo = codigo };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Codigo);
    }

    [Fact]
    public void Codigo_lowercase_aceito_porque_validator_normaliza_para_upper()
    {
        var cmd = NovoComando() with { Codigo = "mtz01" };
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.Codigo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public void CelulasAtivas_invalido_falha(int? celulas)
    {
        var cmd = NovoComando() with { CelulasAtivas = celulas };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.CelulasAtivas);
    }

    [Theory]
    [InlineData("11111111111111")] // DV inválido (sequência repetida)
    [InlineData("1122233300018")] // 13 dígitos
    [InlineData("abc")] // não-numérico
    public void Cnpj_invalido_falha(string cnpj)
    {
        var cmd = NovoComando() with { Cnpj = cnpj };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Cnpj);
    }

    [Fact]
    public void Cnpj_ausente_aceito_quando_outros_campos_validos()
    {
        var cmd = NovoComando() with { Cnpj = null };
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.Cnpj);
    }

    [Fact]
    public void Uf_invalida_falha_no_validator_via_length()
    {
        var cmd = NovoComando() with { Endereco = EnderecoValido() };
        cmd.Endereco!.Uf = "SAO";
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor("Endereco.Uf");
    }

    // C5: UF com exatamente 2 caracteres mas fora da lista das 27 UFs deve falhar
    // antes de o VO `Endereco` lançar `DomainException` (que viraria 500).
    [Theory]
    [InlineData("XX")]
    [InlineData("ZZ")]
    [InlineData("AA")]
    public void Uf_dois_caracteres_fora_da_lista_falha_com_chave_endereco_uf(string uf)
    {
        var cmd = NovoComando() with { Endereco = EnderecoValido() };
        cmd.Endereco!.Uf = uf;
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor("Endereco.Uf");
    }

    [Fact]
    public void Endereco_cep_curto_falha()
    {
        var cmd = NovoComando() with { Endereco = EnderecoValido() };
        cmd.Endereco!.Cep = "123";
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor("Endereco.Cep");
    }

    [Fact]
    public void Endereco_ausente_aceito_payload_minimo()
    {
        var cmd = NovoComando() with { Endereco = null };
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    private static CriarFilialCommand NovoComando() => new(
        Nome: "Filial Matriz",
        Codigo: "MTZ01",
        Cnpj: "11222333000181",
        CelulasAtivas: 30,
        Timezone: null,
        Endereco: null,
        TraceId: "trace-1",
        UsuarioId: null);

    private static EnderecoFilialRequest EnderecoValido() => new()
    {
        Cep = "01310100",
        Logradouro = "Av. Paulista",
        Numero = "1000",
        Bairro = "Bela Vista",
        Cidade = "São Paulo",
        Uf = "SP",
    };
}
