using System.Text.RegularExpressions;
using CarWash.Application.Common;
using CarWash.Domain.Entities;
using FluentValidation;

namespace CarWash.Application.Filiais.Criar;

/// <summary>
/// Validador do RF017 + RF018 (ADR-0007 §5). Nome 3..120, código alfanumérico
/// maiúsculo 2..20, CNPJ opcional com DV, células 1..100 (RN009 / CA008),
/// endereço opcional reutilizando as regras de <c>CriarClienteCommandValidator</c>.
/// </summary>
public sealed class CriarFilialCommandValidator : AbstractValidator<CriarFilialCommand>
{
    private static readonly Regex CodigoRegex = new(
        "^[A-Z0-9]{2,20}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(50));

    public CriarFilialCommandValidator()
    {
        RuleFor(x => x.Nome)
            .Cascade(CascadeMode.Stop)
            .Must(nome => !string.IsNullOrWhiteSpace(InputNormalizer.SanitizeTextOrNull(nome)))
            .WithMessage("O nome é obrigatório.")
            .Must(nome => InputNormalizer.SanitizeTextOrNull(nome)!.Length >= Filial.NomeMinChars)
            .WithMessage($"O nome deve ter no mínimo {Filial.NomeMinChars} caracteres.")
            .Must(nome => InputNormalizer.SanitizeTextOrNull(nome)!.Length <= Filial.NomeMaxChars)
            .WithMessage($"O nome deve ter no máximo {Filial.NomeMaxChars} caracteres.");

        RuleFor(x => x.Codigo)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("O código é obrigatório.")
            .Must(codigo => CodigoRegex.IsMatch(NormalizarCodigo(codigo)))
            .WithMessage("O código deve conter de 2 a 20 caracteres alfanuméricos (A-Z, 0-9).");

        RuleFor(x => x.CelulasAtivas)
            .NotNull()
            .WithMessage("Quantidade de células ativas é obrigatória.");

        RuleFor(x => x.CelulasAtivas)
            .Must(v => !v.HasValue || (v.Value >= Filial.MinCelulasAtivas && v.Value <= Filial.MaxCelulasAtivas))
            .When(x => x.CelulasAtivas.HasValue)
            .WithMessage($"Quantidade de células ativas deve estar entre {Filial.MinCelulasAtivas} e {Filial.MaxCelulasAtivas}.");

        RuleFor(x => x.Cnpj)
            .Cascade(CascadeMode.Stop)
            .Must(InputNormalizer.ContainsOnlyDigits)
            .When(x => !string.IsNullOrWhiteSpace(x.Cnpj))
            .WithMessage("CNPJ deve conter apenas números.")
            .Must(x => InputNormalizer.OnlyDigitsOrNull(x)?.Length == 14)
            .When(x => !string.IsNullOrWhiteSpace(x.Cnpj))
            .WithMessage("CNPJ deve conter 14 dígitos.")
            .Must(DocumentoValidator.CnpjValido)
            .When(x => !string.IsNullOrWhiteSpace(x.Cnpj))
            .WithMessage("CNPJ inválido.");

        RuleFor(x => x.Timezone)
            .MaximumLength(64)
            .When(x => !string.IsNullOrWhiteSpace(x.Timezone))
            .WithMessage("Timezone deve ter no máximo 64 caracteres.");

        When(x => x.Endereco is not null, () =>
        {
            RuleFor(x => x.Endereco!.Cep)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("CEP é obrigatório.")
                .Must(InputNormalizer.ContainsOnlyDigits).WithMessage("CEP deve conter apenas números.")
                .Length(8).WithMessage("CEP deve conter 8 dígitos.");

            RuleFor(x => x.Endereco!.Logradouro)
                .NotEmpty().WithMessage("Logradouro é obrigatório.")
                .MaximumLength(150).WithMessage("Logradouro deve ter no máximo 150 caracteres.");

            RuleFor(x => x.Endereco!.Numero)
                .NotEmpty().WithMessage("Número é obrigatório.")
                .MaximumLength(20).WithMessage("Número deve ter no máximo 20 caracteres.");

            RuleFor(x => x.Endereco!.Complemento)
                .MaximumLength(100)
                .When(x => !string.IsNullOrWhiteSpace(x.Endereco!.Complemento))
                .WithMessage("Complemento deve ter no máximo 100 caracteres.");

            RuleFor(x => x.Endereco!.Bairro)
                .NotEmpty().WithMessage("Bairro é obrigatório.")
                .MaximumLength(100).WithMessage("Bairro deve ter no máximo 100 caracteres.");

            RuleFor(x => x.Endereco!.Cidade)
                .NotEmpty().WithMessage("Cidade é obrigatória.")
                .MaximumLength(100).WithMessage("Cidade deve ter no máximo 100 caracteres.");

            RuleFor(x => x.Endereco!.Uf)
                .NotEmpty().WithMessage("UF é obrigatória.")
                .Length(2).WithMessage("UF deve ter exatamente 2 caracteres.");
        });
    }

    /// <summary>
    /// Normaliza o código informado pelo cliente: trim + UPPER. A defesa de
    /// formato (regex) é feita após a normalização para que "mtz" valide igual
    /// a "MTZ" — o handler persiste o valor normalizado, e o CHECK no banco
    /// rejeita qualquer escrita fora do fluxo.
    /// </summary>
    internal static string NormalizarCodigo(string? codigo)
        => string.IsNullOrWhiteSpace(codigo) ? string.Empty : codigo.Trim().ToUpperInvariant();
}
