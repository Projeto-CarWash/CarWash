using FluentValidation;

namespace CarWash.Application.Clientes.AlterarStatus;

/// <summary>
/// Valida o command de alteração de status. <c>Ativo</c> é nullable
/// — exige presença explícita no body para evitar desativação silenciosa
/// quando o cliente envia <c>{}</c>.
/// </summary>
public sealed class AlterarStatusClienteCommandValidator : AbstractValidator<AlterarStatusClienteCommand>
{
    public const string MensagemClienteIdInvalido = "Identificador do cliente é obrigatório.";
    public const string MensagemAtivoObrigatorio = "Campo 'ativo' é obrigatório.";

    public AlterarStatusClienteCommandValidator()
    {
        RuleFor(x => x.ClienteId)
            .NotEqual(Guid.Empty).WithMessage(MensagemClienteIdInvalido);

        RuleFor(x => x.Ativo)
            .NotNull().WithMessage(MensagemAtivoObrigatorio);
    }
}
