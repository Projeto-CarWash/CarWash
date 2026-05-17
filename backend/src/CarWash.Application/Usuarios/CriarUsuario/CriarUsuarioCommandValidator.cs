using System.Text.RegularExpressions;
using CarWash.Domain.Enums;
using FluentValidation;

namespace CarWash.Application.Usuarios.CriarUsuario;

/// <summary>
/// Política de senha alinhada a NIST SP 800-63B: mínimo 8 caracteres, pelo menos
/// uma letra e um dígito. Não exigir mistura de caixa/especial — diretriz oficial.
/// </summary>
public sealed partial class CriarUsuarioCommandValidator : AbstractValidator<CriarUsuarioCommand>
{
    public const string MensagemSenhaFraca = "Senha não atende aos requisitos mínimos.";
    public const string MensagemPayloadInvalido = "Dados do usuário inválidos. Verifique os campos e tente novamente.";

    public CriarUsuarioCommandValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(120).WithMessage("Nome excede 120 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail é obrigatório.")
            .MaximumLength(150).WithMessage("E-mail excede 150 caracteres.")
            .Must(EmailValido).WithMessage("E-mail inválido.");

        RuleFor(x => x.Senha)
            .NotEmpty().WithMessage(MensagemSenhaFraca)
            .MinimumLength(8).WithMessage(MensagemSenhaFraca)
            .MaximumLength(128).WithMessage(MensagemSenhaFraca)
            .Must(ContemLetra).WithMessage(MensagemSenhaFraca)
            .Must(ContemNumero).WithMessage(MensagemSenhaFraca);

        RuleFor(x => x.Perfil)
            .NotNull().WithMessage("Perfil é obrigatório.")
            .IsInEnum().WithMessage("Perfil inválido.");
    }

    private static bool EmailValido(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        return EmailRegex().IsMatch(email.Trim());
    }

    private static bool ContemLetra(string? senha) =>
        !string.IsNullOrEmpty(senha) && LetraRegex().IsMatch(senha);

    private static bool ContemNumero(string? senha) =>
        !string.IsNullOrEmpty(senha) && DigitoRegex().IsMatch(senha);

    [GeneratedRegex(@"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"[A-Za-z]", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex LetraRegex();

    [GeneratedRegex(@"\d", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex DigitoRegex();
}
