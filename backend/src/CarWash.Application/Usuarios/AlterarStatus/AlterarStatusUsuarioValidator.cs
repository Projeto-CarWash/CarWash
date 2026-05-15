using FluentValidation;

namespace CarWash.Application.Usuarios.AlterarStatus;

/// <summary>
/// Valida o command de alteração de status. <c>Ativo</c> é bool primitivo — o
/// próprio binding garante presença. O foco é o <c>UsuarioId</c>.
/// </summary>
public sealed class AlterarStatusUsuarioValidator : AbstractValidator<AlterarStatusUsuarioCommand>
{
    public const string MensagemUsuarioIdInvalido = "Identificador do usuário é obrigatório.";

    public AlterarStatusUsuarioValidator()
    {
        RuleFor(x => x.UsuarioId)
            .NotEqual(Guid.Empty).WithMessage(MensagemUsuarioIdInvalido);
    }
}
