using FluentValidation;

namespace CarWash.Application.Usuarios.Preferencias.AtualizarTema;

public sealed class AtualizarTemaUsuarioCommandValidator
    : AbstractValidator<AtualizarTemaUsuarioCommand>
{
    public AtualizarTemaUsuarioCommandValidator()
    {
        RuleFor(x => x.UsuarioId)
            .NotEmpty()
            .WithMessage("Usuário autenticado é obrigatório.");

        RuleFor(x => x.Theme)
            .NotEmpty()
            .WithMessage("Tema inválido. Informe light ou dark.")
            .Must(x =>
            {
                string? theme = x?.Trim().ToLowerInvariant();

                return theme is "light" or "dark";
            })
            .WithMessage("Tema inválido. Informe light ou dark.");
    }
}
