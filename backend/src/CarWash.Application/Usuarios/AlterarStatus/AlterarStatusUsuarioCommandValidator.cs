using FluentValidation;

namespace CarWash.Application.Usuarios.AlterarStatus;

/// <summary>
/// Valida o command de alteração de status. <c>Ativo</c> é nullable
/// (<see cref="AlterarStatusUsuarioCommand"/>) — exige presença explícita no body
/// para evitar desativação silenciosa quando o cliente envia <c>{}</c>.
/// </summary>
public sealed class AlterarStatusUsuarioCommandValidator : AbstractValidator<AlterarStatusUsuarioCommand>
{
    public const string MensagemUsuarioIdInvalido = "Identificador do usuário é obrigatório.";
    public const string MensagemAtivoObrigatorio = "Campo 'ativo' é obrigatório.";

    public AlterarStatusUsuarioCommandValidator()
    {
        RuleFor(x => x.UsuarioId)
            .NotEqual(Guid.Empty).WithMessage(MensagemUsuarioIdInvalido);

        RuleFor(x => x.Ativo)
            .NotNull().WithMessage(MensagemAtivoObrigatorio);
    }
}
