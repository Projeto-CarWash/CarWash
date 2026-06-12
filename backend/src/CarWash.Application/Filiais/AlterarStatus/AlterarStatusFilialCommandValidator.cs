using FluentValidation;

namespace CarWash.Application.Filiais.AlterarStatus;

/// <summary>
/// Valida o command de alteração de status da filial. <c>Ativo</c> é nullable
/// (<see cref="AlterarStatusFilialCommand"/>) — exige presença explícita no
/// body para evitar inativação silenciosa quando o cliente envia <c>{}</c>.
/// </summary>
public sealed class AlterarStatusFilialCommandValidator : AbstractValidator<AlterarStatusFilialCommand>
{
    public const string MensagemFilialIdInvalido = "Identificador da filial é obrigatório.";
    public const string MensagemAtivoObrigatorio = "Campo 'ativo' é obrigatório.";

    public AlterarStatusFilialCommandValidator()
    {
        RuleFor(x => x.FilialId)
            .NotEqual(Guid.Empty).WithMessage(MensagemFilialIdInvalido);

        RuleFor(x => x.Ativo)
            .NotNull().WithMessage(MensagemAtivoObrigatorio);
    }
}
