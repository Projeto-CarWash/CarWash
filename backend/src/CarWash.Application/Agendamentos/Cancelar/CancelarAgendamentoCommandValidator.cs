using FluentValidation;

namespace CarWash.Application.Agendamentos.Cancelar;

/// <summary>
/// Validador estrutural do RF010. Garante agendamentoId preenchido,
/// motivoCancelamento obrigatório (trim, 5–500 chars) e origem informada.
/// Regras de estado (status permitido) são verificadas no handler/domínio.
/// </summary>
public sealed class CancelarAgendamentoCommandValidator : AbstractValidator<CancelarAgendamentoCommand>
{
	public CancelarAgendamentoCommandValidator()
	{
		RuleFor(x => x.AgendamentoId)
			.NotEmpty().WithMessage("Identificador do agendamento é obrigatório.");

		RuleFor(x => x.MotivoCancelamento)
			.Cascade(CascadeMode.Stop)
			.NotEmpty().WithMessage("Motivo do cancelamento é obrigatório.")
			.Must(m => m.Trim().Length >= 5).WithMessage("Motivo do cancelamento deve ter no mínimo 5 caracteres.")
			.Must(m => m.Trim().Length <= 500).WithMessage("Motivo do cancelamento deve ter no máximo 500 caracteres.");

		RuleFor(x => x.Origem)
			.NotEmpty().WithMessage("Origem do cancelamento é obrigatória.");
	}
}
