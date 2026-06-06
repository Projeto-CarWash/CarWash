using System.Globalization;
using CarWash.Application.Agendamentos.Common;
using FluentAssertions;
using Xunit;

namespace CarWash.UnitTests.Application.Agendamentos;

/// <summary>
/// Testes do <c>hashResumo</c> (RF015 / ADR 0004) — determinismo e sensibilidade
/// à mudança de cada campo de negócio que entra na forma canônica.
/// </summary>
public class HashResumoAgendamentoTests
{
    private static readonly Guid Filial = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Cliente = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Veiculo = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ServicoA = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid ServicoB = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid Responsavel = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly DateTime Inicio = new(2099, 6, 1, 14, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Hash_e_determinista_para_a_mesma_entrada()
    {
        string hash1 = Calcular();
        string hash2 = Calcular();

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void Hash_e_sha256_hex_minusculo_de_64_caracteres()
    {
        string hash = Calcular();

        hash.Should().HaveLength(64);
        hash.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void Ordem_dos_servicos_nao_altera_o_hash()
    {
        // A canônica ordena os servicoIds — ordens diferentes do mesmo conjunto
        // produzem o mesmo hash.
        string hashAB = Calcular(servicos: new[] { ServicoA, ServicoB });
        string hashBA = Calcular(servicos: new[] { ServicoB, ServicoA });

        hashAB.Should().Be(hashBA);
    }

    [Fact]
    public void Mudanca_de_filial_altera_o_hash()
    {
        Calcular(filial: Guid.NewGuid()).Should().NotBe(Calcular());
    }

    [Fact]
    public void Mudanca_de_inicio_altera_o_hash()
    {
        Calcular(inicio: Inicio.AddHours(1)).Should().NotBe(Calcular());
    }

    [Fact]
    public void Mudanca_de_valor_total_altera_o_hash()
    {
        Calcular(valorTotal: 200m).Should().NotBe(Calcular());
    }

    [Fact]
    public void Mudanca_de_duracao_altera_o_hash()
    {
        Calcular(duracao: 90).Should().NotBe(Calcular());
    }

    [Fact]
    public void Mudanca_de_responsavel_altera_o_hash()
    {
        string hashA = Calcular(responsavel: Responsavel);
        string hashB = Calcular(responsavel: Guid.NewGuid());

        hashA.Should().NotBe(hashB);
    }

    [Fact]
    public void Mudanca_de_observacoes_altera_o_hash()
    {
        Calcular(observacoes: "Outra observação").Should().NotBe(Calcular(observacoes: "Original"));
    }

    [Fact]
    public void Observacoes_nula_e_em_branco_produzem_o_mesmo_hash()
    {
        // SanitizeTextOrNull normaliza ambos para o literal "null" na canônica.
        Calcular(observacoes: null).Should().Be(Calcular(observacoes: "   "));
    }

    [Fact]
    public void Valor_total_e_formatado_com_duas_casas_invariant()
    {
        // 125 e 125.00 representam o mesmo valor decimal — hash idêntico.
        Calcular(valorTotal: 125m).Should().Be(Calcular(valorTotal: 125.00m));
    }

    [Fact]
    public void Inicio_em_horario_local_e_normalizado_para_utc_antes_do_hash()
    {
        var inicioUtc = new DateTime(2099, 6, 1, 17, 0, 0, DateTimeKind.Utc);
        var inicioLocal = inicioUtc.ToLocalTime();

        Calcular(inicio: inicioLocal).Should().Be(Calcular(inicio: inicioUtc));
    }

    private static string Calcular(
        Guid? filial = null,
        Guid? responsavel = null,
        IReadOnlyList<Guid>? servicos = null,
        DateTime? inicio = null,
        int duracao = 75,
        decimal valorTotal = 100m,
        string? observacoes = "Original")
    {
        return CalculadoraResumoAgendamento.CalcularHashResumo(
            filialId: filial ?? Filial,
            clienteId: Cliente,
            veiculoId: Veiculo,
            responsavelId: responsavel ?? Responsavel,
            servicoIds: servicos ?? new[] { ServicoA, ServicoB },
            inicioUtc: inicio ?? Inicio,
            duracaoTotalMin: duracao,
            valorTotal: valorTotal,
            observacoes: observacoes);
    }

    [Fact]
    public void Forma_canonica_documentada_produz_o_hash_esperado()
    {
        // Regressão: garante que a string canônica não muda silenciosamente.
        var responsavelCanonico = Guid.Parse("66666666-6666-6666-6666-666666666666");

        string hash = CalculadoraResumoAgendamento.CalcularHashResumo(
            filialId: Filial,
            clienteId: Cliente,
            veiculoId: Veiculo,
            responsavelId: responsavelCanonico,
            servicoIds: new[] { ServicoA },
            inicioUtc: Inicio,
            duracaoTotalMin: 30,
            valorTotal: 50m,
            observacoes: null);

        // Calculado a partir da canônica definida no ADR 0004:
        // filial|cliente|veiculo|responsavel|servico|2099-06-01T14:00:00.000Z|30|50.00|null
        string canonico = string.Join(
            '|',
            Filial.ToString("D", CultureInfo.InvariantCulture),
            Cliente.ToString("D", CultureInfo.InvariantCulture),
            Veiculo.ToString("D", CultureInfo.InvariantCulture),
            responsavelCanonico.ToString("D", CultureInfo.InvariantCulture),
            ServicoA.ToString("D", CultureInfo.InvariantCulture),
            "2099-06-01T14:00:00.000Z",
            "30",
            "50.00",
            "null");
        string esperado = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(canonico)))
            .ToLowerInvariant();

        hash.Should().Be(esperado);
    }
}
