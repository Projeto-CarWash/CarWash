using CarWash.Application.Agendamentos.PreConfirmar;
using FluentAssertions;
using Xunit;

namespace CarWash.UnitTests.Application.Agendamentos;

public class PreConfirmarAgendamentoCommandValidatorTests
{
    private readonly PreConfirmarAgendamentoCommandValidator _validator = new();

    [Fact]
    public void Comando_valido_passa()
    {
        var resultado = _validator.Validate(ComandoValido());
        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Inicio_hoje_hora_futura_passa()
    {
        var inicio = DateTime.UtcNow.AddHours(2);
        var resultado = _validator.Validate(ComandoValido() with { Inicio = inicio });
        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Inicio_no_passado_mais_de_1_minuto_falha()
    {
        var resultado = _validator.Validate(ComandoValido() with { Inicio = DateTime.UtcNow.AddMinutes(-5) });
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e =>
            e.PropertyName == nameof(PreConfirmarAgendamentoCommand.Inicio)
            && e.ErrorMessage == "A data/hora de início não pode estar no passado.");
    }

    [Fact]
    public void Inicio_nulo_falha()
    {
        var resultado = _validator.Validate(ComandoValido() with { Inicio = null });
        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Filial_vazia_falha()
    {
        var resultado = _validator.Validate(ComandoValido() with { FilialId = Guid.Empty });
        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Cliente_vazio_falha()
    {
        var resultado = _validator.Validate(ComandoValido() with { ClienteId = Guid.Empty });
        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Veiculo_vazio_falha()
    {
        var resultado = _validator.Validate(ComandoValido() with { VeiculoId = Guid.Empty });
        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Responsavel_vazio_falha()
    {
        var resultado = _validator.Validate(ComandoValido() with { ResponsavelId = Guid.Empty });
        resultado.IsValid.Should().BeFalse();
    }

    private static PreConfirmarAgendamentoCommand ComandoValido() => new(
        FilialId: Guid.NewGuid(),
        ClienteId: Guid.NewGuid(),
        VeiculoId: Guid.NewGuid(),
        ResponsavelId: Guid.NewGuid(),
        Inicio: DateTime.UtcNow.AddDays(1),
        ServicoIds: new[] { Guid.NewGuid() },
        Observacoes: null,
        TraceId: "trace-1",
        UsuarioId: Guid.NewGuid());
}
