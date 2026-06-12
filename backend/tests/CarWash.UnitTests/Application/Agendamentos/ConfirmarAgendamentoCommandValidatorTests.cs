using CarWash.Application.Agendamentos.Confirmar;
using FluentAssertions;
using Xunit;

namespace CarWash.UnitTests.Application.Agendamentos;

public class ConfirmarAgendamentoCommandValidatorTests
{
    private readonly ConfirmarAgendamentoCommandValidator _validator = new();

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
            e.PropertyName == nameof(ConfirmarAgendamentoCommand.Inicio)
            && e.ErrorMessage == "A data/hora de início não pode estar no passado.");
    }

    [Fact]
    public void Inicio_nulo_falha()
    {
        var resultado = _validator.Validate(ComandoValido() with { Inicio = null });
        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Confirmar_ausente_falha()
    {
        var resultado = _validator.Validate(ComandoValido() with { Confirmar = null });
        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Confirmar_false_falha()
    {
        var resultado = _validator.Validate(ComandoValido() with { Confirmar = false });
        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public void TokenConfirmacao_vazio_falha()
    {
        var resultado = _validator.Validate(ComandoValido() with { TokenConfirmacao = string.Empty });
        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IdempotencyKey_vazio_falha()
    {
        var resultado = _validator.Validate(ComandoValido() with { IdempotencyKey = Guid.Empty });
        resultado.IsValid.Should().BeFalse();
    }

    private static ConfirmarAgendamentoCommand ComandoValido() => new(
        FilialId: Guid.NewGuid(),
        ClienteId: Guid.NewGuid(),
        VeiculoId: Guid.NewGuid(),
        ResponsavelId: Guid.NewGuid(),
        Inicio: DateTime.UtcNow.AddDays(1),
        ServicoIds: new[] { Guid.NewGuid() },
        Observacoes: null,
        Confirmar: true,
        TokenConfirmacao: "token.valido",
        IdempotencyKey: Guid.NewGuid(),
        TraceId: "trace-1",
        UsuarioId: Guid.NewGuid());
}
