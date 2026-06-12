using FluentValidation;

namespace CarWash.Application.Responsaveis.AlterarStatus;

public sealed class AlterarStatusResponsavelCommandValidator : AbstractValidator<AlterarStatusResponsavelCommand>
{
    public const string MensagemResponsavelIdInvalido = "Identificador do responsável é obrigatório.";
    public const string MensagemClienteTitularIdInvalido = "Identificador do cliente titular é obrigatório.";
    public const string MensagemAtivoObrigatorio = "Campo 'ativo' é obrigatório.";

    public AlterarStatusResponsavelCommandValidator()
    {
        RuleFor(x => x.ResponsavelId)
            .NotEqual(Guid.Empty).WithMessage(MensagemResponsavelIdInvalido);

        RuleFor(x => x.ClienteTitularId)
            .NotEqual(Guid.Empty).WithMessage(MensagemClienteTitularIdInvalido);

        RuleFor(x => x.Ativo)
            .NotNull().WithMessage(MensagemAtivoObrigatorio);
    }
}
