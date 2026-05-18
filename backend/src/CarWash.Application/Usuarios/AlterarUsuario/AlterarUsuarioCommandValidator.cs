using FluentValidation;

namespace CarWash.Application.Usuarios.AlterarUsuario;

public sealed class AlterarUsuarioCommandValidator : AbstractValidator<AlterarUsuarioCommand>
{
    public AlterarUsuarioCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty).WithMessage("Id do usuário é obrigatório.");

        RuleFor(x => x.Nome)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MinimumLength(3).WithMessage("Nome deve ter no mínimo 3 caracteres.")
            .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("E-mail é obrigatório.")
            .MinimumLength(5).WithMessage("E-mail deve ter no mínimo 5 caracteres.")
            .MaximumLength(150).WithMessage("E-mail deve ter no máximo 150 caracteres.")
            .EmailAddress().WithMessage("E-mail inválido.");

        RuleFor(x => x.Perfil)
            .IsInEnum().WithMessage("Perfil inválido.");
    }
}
