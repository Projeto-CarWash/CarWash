using CarWash.Application.Common;
using CarWash.Domain.Enums;
using FluentValidation;

namespace CarWash.Application.Responsaveis.Criar;

public sealed class CriarResponsavelCommandValidator : AbstractValidator<CriarResponsavelCommand>
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

    public CriarResponsavelCommandValidator()
    {
        RuleFor(x => x.ClienteTitularId)
            .NotEmpty().WithMessage("Identificador do cliente titular é obrigatório.");

        RuleFor(x => x.Nome)
            .Cascade(CascadeMode.Stop)
            .Must(nome => !string.IsNullOrWhiteSpace(InputNormalizer.SanitizeTextOrNull(nome)))
            .WithMessage("O nome é obrigatório.")
            .Must(nome => InputNormalizer.SanitizeTextOrNull(nome)!.Length >= 3)
            .WithMessage("O nome deve ter no mínimo 3 caracteres.")
            .Must(nome => InputNormalizer.SanitizeTextOrNull(nome)!.Length <= 100)
            .WithMessage("O nome deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Documento)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("O documento é obrigatório.")
            .Must(InputNormalizer.ContainsOnlyDigits)
            .WithMessage("Documento deve conter apenas números.")
            .Must(DocumentoValido)
            .WithMessage("Documento inválido.");

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

    private static bool DocumentoValido(string? documento)
    {
        string? apenasDigitos = InputNormalizer.OnlyDigitsOrNull(documento);
        if (apenasDigitos is null)
        {
            return false;
        }

        if (apenasDigitos.Length == 11)
        {
            return DocumentoValidator.CpfValido(apenasDigitos);
        }

        if (apenasDigitos.Length == 14)
        {
            return DocumentoValidator.CnpjValido(apenasDigitos);
        }

        return false;
    }
}
