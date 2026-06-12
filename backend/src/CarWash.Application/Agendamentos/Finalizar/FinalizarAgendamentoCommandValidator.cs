using FluentValidation;

namespace CarWash.Application.Agendamentos.Finalizar;

/// <summary>
/// Validador estrutural da finalização de atendimento. Regras de estado
/// (status atual permitido) são verificadas no handler/domínio.
/// </summary>
public sealed class FinalizarAgendamentoCommandValidator : AbstractValidator<FinalizarAgendamentoCommand>
{
    public FinalizarAgendamentoCommandValidator()
    {
        RuleFor(x => x.AgendamentoId)
            .NotEmpty().WithMessage("Identificador do agendamento é obrigatório.");
    }
}
