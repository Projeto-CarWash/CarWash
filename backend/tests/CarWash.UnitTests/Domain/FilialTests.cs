using CarWash.Domain.Common;
using CarWash.Domain.Entities;
using CarWash.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace CarWash.UnitTests.Domain;

/// <summary>
/// Testes de invariantes do agregado <see cref="Filial"/> (RF017 + RF018 +
/// RN009). Cobertura mínima exigida pelo ADR-0007 §8.1 (BE-15).
/// </summary>
public class FilialTests
{
    private const string CodigoValido = "MTZ01";
    private const string NomeValido = "Filial Matriz";

    [Fact]
    public void Criar_caminho_feliz_inicia_ativa_e_em_sao_paulo()
    {
        var filial = Filial.Criar(Guid.NewGuid(), NomeValido, CodigoValido, 10);

        filial.Ativa.Should().BeTrue();
        filial.Timezone.Should().Be("America/Sao_Paulo");
        filial.Nome.Should().Be(NomeValido);
        filial.Codigo.Should().Be(CodigoValido);
        filial.CelulasAtivas.Should().Be(10);
        filial.CriadoEm.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        filial.AtualizadoEm.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Criar_rejeita_id_vazio()
    {
        var act = () => Filial.Criar(Guid.Empty, NomeValido, CodigoValido, 10);
        act.Should().Throw<DomainException>().WithMessage("*Id da filial*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ab")] // < 3 chars
    public void Criar_rejeita_nome_invalido(string? nome)
    {
        var act = () => Filial.Criar(Guid.NewGuid(), nome!, CodigoValido, 10);
        act.Should().Throw<DomainException>().WithMessage("*Nome*");
    }

    [Fact]
    public void Criar_rejeita_nome_acima_de_120_chars()
    {
        var nomeLongo = new string('A', 121);
        var act = () => Filial.Criar(Guid.NewGuid(), nomeLongo, CodigoValido, 10);
        act.Should().Throw<DomainException>().WithMessage("*Nome*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")] // < 2
    [InlineData("a23")] // minúscula não passa
    [InlineData("MTZ-01")] // hífen não passa
    [InlineData("ABCDEFGHIJKLMNOPQRSTU")] // 21 chars
    [InlineData("MTZ 01")] // espaço não passa
    public void Criar_rejeita_codigo_invalido(string codigo)
    {
        var act = () => Filial.Criar(Guid.NewGuid(), NomeValido, codigo, 10);
        act.Should().Throw<DomainException>().WithMessage("*ódigo*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(1000)]
    public void Criar_rejeita_celulas_fora_da_faixa_RN009(int celulas)
    {
        var act = () => Filial.Criar(Guid.NewGuid(), NomeValido, CodigoValido, celulas);
        act.Should().Throw<DomainException>().WithMessage("*RN009*");
    }

    [Fact]
    public void Criar_com_endereco_e_cnpj_preserva_valores()
    {
        var endereco = new Endereco(
            cep: "01310100",
            logradouro: "Av. Paulista",
            numero: "1000",
            complemento: "Sala 12",
            bairro: "Bela Vista",
            cidade: "São Paulo",
            uf: "SP");
        var cnpj = new Cnpj("11222333000181");

        var filial = Filial.Criar(Guid.NewGuid(), NomeValido, CodigoValido, 50, endereco, cnpj, "America/Recife");

        filial.Cnpj.Should().Be("11222333000181");
        filial.Timezone.Should().Be("America/Recife");
        filial.EnderecoCep.Should().Be("01310100");
        filial.EnderecoLogradouro.Should().Be("Av. Paulista");
        filial.EnderecoNumero.Should().Be("1000");
        filial.EnderecoComplemento.Should().Be("Sala 12");
        filial.EnderecoBairro.Should().Be("Bela Vista");
        filial.EnderecoCidade.Should().Be("São Paulo");
        filial.EnderecoUf.Should().Be("SP");
        filial.Endereco.Should().NotBeNull();
        filial.Endereco!.Cidade.Should().Be("São Paulo");
    }

    [Fact]
    public void Endereco_getter_retorna_null_quando_nao_informado()
    {
        var filial = Filial.Criar(Guid.NewGuid(), NomeValido, CodigoValido, 10);
        filial.Endereco.Should().BeNull();
    }

    [Fact]
    public void RegistrarCriadoPor_aplica_usuario()
    {
        var filial = Filial.Criar(Guid.NewGuid(), NomeValido, CodigoValido, 10);
        var usuarioId = Guid.NewGuid();

        filial.RegistrarCriadoPor(usuarioId);

        filial.CriadoPorUsuarioId.Should().Be(usuarioId);
    }

    [Fact]
    public void Inativar_e_Ativar_alteram_status()
    {
        var filial = Filial.Criar(Guid.NewGuid(), NomeValido, CodigoValido, 10);

        filial.Inativar();
        filial.Ativa.Should().BeFalse();

        filial.Ativar();
        filial.Ativa.Should().BeTrue();
    }

    [Fact]
    public void AjustarCelulas_aceita_dentro_da_faixa_e_rejeita_fora_RN009()
    {
        var filial = Filial.Criar(Guid.NewGuid(), NomeValido, CodigoValido, 10);

        filial.AjustarCelulas(50);
        filial.CelulasAtivas.Should().Be(50);

        var act = () => filial.AjustarCelulas(0);
        act.Should().Throw<DomainException>().WithMessage("*RN009*");

        var act2 = () => filial.AjustarCelulas(101);
        act2.Should().Throw<DomainException>().WithMessage("*RN009*");
    }
}
