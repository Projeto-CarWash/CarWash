using FluentValidation;

namespace CarWash.Application.Servicos.AlterarStatus;

/// <summary>
/// Valida o command de alteração de status. <c>Ativo</c> é nullable
/// — exige presença explícita no body para evitar desativação silenciosa
/// quando o cliente envia <c>{}</c>.
/// </summary>
public sealed class AlterarStatusServicoCommandValidator : AbstractValidator<AlterarStatusServicoCommand>
{
    public const string MensagemServicoIdInvalido = "Identificador do serviço é obrigatório.";
    public const string MensagemAtivoObrigatorio = "Campo 'ativo' é obrigatório.";

    public AlterarStatusServicoCommandValidator()
    {
        RuleFor(x => x.ServicoId)
            .NotEqual(Guid.Empty).WithMessage(MensagemServicoIdInvalido);

        RuleFor(x => x.Ativo)
            .NotNull().WithMessage(MensagemAtivoObrigatorio);
    }
}
