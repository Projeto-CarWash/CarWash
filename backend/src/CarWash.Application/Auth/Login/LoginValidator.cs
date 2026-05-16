using FluentValidation;

namespace CarWash.Application.Auth.Login;

/// <summary>
/// Validação mínima do payload de login. <strong>Não</strong> valida formato de
/// e-mail — devolver 400 para e-mail malformado criaria oráculo de enumeração;
/// o handler trata como 401 unificado.
/// </summary>
public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public const string MensagemEmailObrigatorio = "E-mail é obrigatório.";
    public const string MensagemSenhaObrigatoria = "Senha é obrigatória.";

    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(MensagemEmailObrigatorio)
            .MaximumLength(150).WithMessage("E-mail excede 150 caracteres.");

        RuleFor(x => x.Senha)
            .NotEmpty().WithMessage(MensagemSenhaObrigatoria)
            .MaximumLength(256).WithMessage("Senha excede 256 caracteres.");
    }
}
