using FluentValidation;

namespace CarWash.Application.Agendamentos.Iniciar;

/// <summary>
/// Validador estrutural do início de atendimento. Regras de estado
/// (status atual permitido) são verificadas no handler/domínio.
/// </summary>
public sealed class IniciarAgendamentoCommandValidator : AbstractValidator<IniciarAgendamentoCommand>
{
    public IniciarAgendamentoCommandValidator()
    {
        RuleFor(x => x.AgendamentoId)
            .NotEmpty().WithMessage("Identificador do agendamento é obrigatório.");
    }
}
