using FluentValidation;

namespace CarWash.Application.Agendamentos.Editar;

/// <summary>
/// Validador estrutural da edição de agendamento (RF010).
/// Garante agendamentoId preenchido. Regras de estado (status permitido)
/// são verificadas no handler/domínio.
/// </summary>
public sealed class EditarAgendamentoCommandValidator : AbstractValidator<EditarAgendamentoCommand>
{
    public EditarAgendamentoCommandValidator()
    {
        RuleFor(x => x.AgendamentoId)
            .NotEmpty().WithMessage("Identificador do agendamento é obrigatório.");
    }
}
