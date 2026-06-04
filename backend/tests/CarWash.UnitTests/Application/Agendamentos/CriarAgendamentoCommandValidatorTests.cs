using CarWash.Application.Agendamentos.Common;
using CarWash.Application.Agendamentos.Criar;
using FluentAssertions;
using Xunit;

namespace CarWash.UnitTests.Application.Agendamentos;

public class CriarAgendamentoCommandValidatorTests
{
    private readonly CriarAgendamentoCommandValidator _validator = new();

    [Fact]
    public void Comando_valido_passa()
    {
        var resultado = _validator.Validate(ComandoValido());
        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Filial_vazia_falha_RF019_com_mensagem_do_card()
    {
        var resultado = _validator.Validate(ComandoValido() with { FilialId = Guid.Empty });

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(CriarAgendamentoCommand.FilialId));
        resultado.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CriarAgendamentoCommand.FilialId)
            && e.ErrorMessage == MensagensFilialAgendamento.Obrigatoria);
    }

    [Fact]
    public void Cliente_vazio_falha()
    {
        var resultado = _validator.Validate(ComandoValido() with { ClienteId = Guid.Empty });

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(CriarAgendamentoCommand.ClienteId));
    }

    [Fact]
    public void Veiculo_vazio_falha()
    {
        var resultado = _validator.Validate(ComandoValido() with { VeiculoId = Guid.Empty });
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(CriarAgendamentoCommand.VeiculoId));
    }

    [Fact]
    public void Inicio_nulo_falha()
    {
        var resultado = _validator.Validate(ComandoValido() with { Inicio = null });
        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Inicio_no_passado_falha()
    {
        var resultado = _validator.Validate(ComandoValido() with { Inicio = DateTime.UtcNow.AddHours(-1) });
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.ErrorMessage.Contains("passado", StringComparison.Ordinal));
    }

    [Fact]
    public void Inicio_agora_passa()
    {
        var resultado = _validator.Validate(ComandoValido() with { Inicio = DateTime.UtcNow });
        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Lista_de_servicos_vazia_falha()
    {
        var resultado = _validator.Validate(ComandoValido() with { ServicoIds = Array.Empty<Guid>() });
        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Lista_de_servicos_nula_falha()
    {
        var resultado = _validator.Validate(ComandoValido() with { ServicoIds = null });
        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Servico_duplicado_falha_CA007()
    {
        var servico = Guid.NewGuid();
        var resultado = _validator.Validate(ComandoValido() with { ServicoIds = new[] { servico, servico } });

        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.ErrorMessage.Contains("CA007", StringComparison.Ordinal));
    }

    [Fact]
    public void Servico_id_vazio_falha()
    {
        var resultado = _validator.Validate(ComandoValido() with { ServicoIds = new[] { Guid.Empty } });
        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Observacoes_muito_longas_falha()
    {
        var resultado = _validator.Validate(ComandoValido() with { Observacoes = new string('x', 1001) });
        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Observacoes_com_exatamente_1000_caracteres_passa()
    {
        var resultado = _validator.Validate(ComandoValido() with { Observacoes = new string('x', 1000) });
        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Observacoes_nulas_passam()
    {
        var resultado = _validator.Validate(ComandoValido() with { Observacoes = null });
        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Multiplos_servicos_distintos_passam()
    {
        var resultado = _validator.Validate(ComandoValido() with
        {
            ServicoIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },
        });
        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Responsavel_id_vazio_falha()
    {
        var resultado = _validator.Validate(ComandoValido() with { ResponsavelId = Guid.Empty });
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.PropertyName == nameof(CriarAgendamentoCommand.ResponsavelId));
    }

    [Fact]
    public void Inicio_no_passado_distante_falha()
    {
        var resultado = _validator.Validate(ComandoValido() with { Inicio = DateTime.UtcNow.AddYears(-1) });
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.ErrorMessage.Contains("passado", StringComparison.Ordinal));
    }

    private static CriarAgendamentoCommand ComandoValido() => new(
        FilialId: Guid.NewGuid(),
        ClienteId: Guid.NewGuid(),
        VeiculoId: Guid.NewGuid(),
        ResponsavelId: Guid.NewGuid(),
        Inicio: DateTime.UtcNow.AddHours(1),
        ServicoIds: new[] { Guid.NewGuid() },
        Observacoes: null,
        TraceId: "trace-1",
        UsuarioId: Guid.NewGuid());
}
