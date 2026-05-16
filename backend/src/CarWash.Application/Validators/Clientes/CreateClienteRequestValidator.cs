using CarWash.Application.Common;
using CarWash.Application.DTOs.Clientes;
using FluentValidation;

namespace CarWash.Application.Validators.Clientes;

public class CreateClienteRequestValidator : AbstractValidator<CreateClienteRequest>
{
    public CreateClienteRequestValidator()
    {
        RuleFor(x => x.Nome)
            .Cascade(CascadeMode.Stop)
            .Must(nome => !string.IsNullOrWhiteSpace(InputNormalizer.TrimOrNull(nome)))
            .WithMessage("O nome é obrigatório.")
            .Must(nome => InputNormalizer.TrimOrNull(nome)!.Length >= 3)
            .WithMessage("O nome deve ter no mínimo 3 caracteres.")
            .Must(nome => InputNormalizer.TrimOrNull(nome)!.Length <= 100)
            .WithMessage("O nome deve ter no máximo 100 caracteres.");

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Cpf) || !string.IsNullOrWhiteSpace(x.Cnpj))
            .WithName("documento")
            .WithMessage("Informe CPF ou CNPJ.");

        RuleFor(x => x)
            .Must(x => string.IsNullOrWhiteSpace(x.Cpf) || string.IsNullOrWhiteSpace(x.Cnpj))
            .WithName("documento")
            .WithMessage("Informe apenas CPF ou CNPJ, não ambos.");

        RuleFor(x => x.Cpf)
            .Must(DocumentoValidator.CpfValido)
            .When(x => !string.IsNullOrWhiteSpace(x.Cpf))
            .WithMessage("CPF inválido.");

        RuleFor(x => x.Cnpj)
            .Must(DocumentoValidator.CnpjValido)
            .When(x => !string.IsNullOrWhiteSpace(x.Cnpj))
            .WithMessage("CNPJ inválido.");

        RuleFor(x => x)
            .Must(x => InputNormalizer.OnlyDigitsOrNull(x.Telefone) is not null || InputNormalizer.OnlyDigitsOrNull(x.Celular) is not null)
            .WithName("contato")
            .WithMessage("Informe telefone ou celular.");

        RuleFor(x => x.Telefone)
            .Cascade(CascadeMode.Stop)
            .Must(InputNormalizer.ContainsOnlyDigits)
            .When(x => !string.IsNullOrWhiteSpace(x.Telefone))
            .WithMessage("Telefone deve conter apenas números.")
            .Must(x => InputNormalizer.OnlyDigitsOrNull(x) is null || InputNormalizer.OnlyDigitsOrNull(x)!.Length is >= 10 and <= 11)
            .WithMessage("Telefone deve conter entre 10 e 11 dígitos.");

        RuleFor(x => x.Celular)
            .Cascade(CascadeMode.Stop)
            .Must(InputNormalizer.ContainsOnlyDigits)
            .When(x => !string.IsNullOrWhiteSpace(x.Celular))
            .WithMessage("Celular deve conter apenas números.")
            .Must(x => InputNormalizer.OnlyDigitsOrNull(x) is null || InputNormalizer.OnlyDigitsOrNull(x)!.Length == 11)
            .WithMessage("Celular deve conter 11 dígitos.");

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("E-mail inválido.")
            .MaximumLength(150)
            .WithMessage("E-mail deve ter no máximo 150 caracteres.");

        RuleFor(x => x.Endereco)
            .MaximumLength(255)
            .WithMessage("Endereço deve ter no máximo 255 caracteres.");

        RuleFor(x => x.Observacoes)
            .MaximumLength(5000)
            .WithMessage("Observações deve ter no máximo 5000 caracteres.");
    }
}
