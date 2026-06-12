using CarWash.Application.Common;
using FluentValidation;

namespace CarWash.Application.Responsaveis.Atualizar;

public sealed class AtualizarResponsavelCommandValidator : AbstractValidator<AtualizarResponsavelCommand>
{
    private static readonly string[] GrausVinculoValidos =
    [
        "RESPONSAVEL_FINANCEIRO",
        "RESPONSAVEL_LEGAL",
        "PROCURADOR",
        "CONJUGE",
        "PAI_MAE",
        "OUTRO",
    ];

    public AtualizarResponsavelCommandValidator()
    {
        RuleFor(x => x.ResponsavelId)
            .NotEqual(Guid.Empty).WithMessage("Identificador do responsável é obrigatório.");

        RuleFor(x => x.ClienteTitularId)
            .NotEqual(Guid.Empty).WithMessage("Identificador do cliente titular é obrigatório.");

        RuleFor(x => x.Nome)
            .Cascade(CascadeMode.Stop)
            .Must(nome => !string.IsNullOrWhiteSpace(InputNormalizer.SanitizeTextOrNull(nome)))
            .WithMessage("O nome é obrigatório.")
            .Must(nome => InputNormalizer.SanitizeTextOrNull(nome)!.Length >= 3)
            .WithMessage("O nome deve ter no mínimo 3 caracteres.")
            .Must(nome => InputNormalizer.SanitizeTextOrNull(nome)!.Length <= 100)
            .WithMessage("O nome deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Telefone)
            .Cascade(CascadeMode.Stop)
            .Must(InputNormalizer.ContainsOnlyDigits)
            .When(x => !string.IsNullOrWhiteSpace(x.Telefone))
            .WithMessage("Telefone deve conter apenas números.")
            .Must(x => InputNormalizer.OnlyDigitsOrNull(x) is null
                || InputNormalizer.OnlyDigitsOrNull(x)!.Length is >= 10 and <= 11)
            .When(x => !string.IsNullOrWhiteSpace(x.Telefone))
            .WithMessage("Telefone deve conter entre 10 e 11 dígitos.");

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .MinimumLength(5)
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("E-mail deve ter no mínimo 5 caracteres.")
            .MaximumLength(150)
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("E-mail deve ter no máximo 150 caracteres.")
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("E-mail inválido.");

        RuleFor(x => x.GrauVinculo)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Grau de vínculo é obrigatório.")
            .Must(g => GrausVinculoValidos.Contains(g, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Grau de vínculo inválido. Valores aceitos: {string.Join(", ", GrausVinculoValidos)}.");
    }
}
