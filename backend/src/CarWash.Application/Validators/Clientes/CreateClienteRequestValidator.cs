using CarWash.Application.DTOs.Clientes;
using CarWash.Application.Common;
using FluentValidation;

namespace CarWash.Application.Validators.Clientes;

public class CreateClienteRequestValidator : AbstractValidator<CreateClienteRequest>
{
    public CreateClienteRequestValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty()
            .WithMessage("O nome é obrigatório.")
            .MaximumLength(100)
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
            .Must(x => !string.IsNullOrWhiteSpace(x.Telefone) || !string.IsNullOrWhiteSpace(x.Celular))
            .WithName("contato")
            .WithMessage("Informe telefone ou celular.");

        RuleFor(x => x.Telefone)
            .Must(x => InputNormalizer.OnlyDigitsOrNull(x) is null || InputNormalizer.OnlyDigitsOrNull(x)!.Length is >= 10 and <= 11)
            .WithMessage("Telefone deve conter entre 10 e 11 dígitos.");

        RuleFor(x => x.Celular)
            .Must(x => InputNormalizer.OnlyDigitsOrNull(x) is null || InputNormalizer.OnlyDigitsOrNull(x)!.Length is 11)
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
