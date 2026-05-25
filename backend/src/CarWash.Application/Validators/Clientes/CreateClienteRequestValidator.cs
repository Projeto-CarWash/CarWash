using CarWash.Application.DTOs.Clientes;
using CarWash.Application.Common;
using FluentValidation;
using System.Text.RegularExpressions;

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
            .Cascade(CascadeMode.Stop)
            .Must(InputNormalizer.ContainsOnlyDigits)
            .When(x => !string.IsNullOrWhiteSpace(x.Cpf))
            .WithMessage("CPF deve conter apenas números.")
            .Length(11)
            .When(x => !string.IsNullOrWhiteSpace(x.Cpf))
            .WithMessage("CPF deve conter 11 dígitos.")
            .Must(DocumentoValidator.CpfValido)
            .When(x => !string.IsNullOrWhiteSpace(x.Cpf))
            .WithMessage("CPF inválido.");

        RuleFor(x => x.Cnpj)
            .Cascade(CascadeMode.Stop)
            .Must(InputNormalizer.ContainsOnlyDigits)
            .When(x => !string.IsNullOrWhiteSpace(x.Cnpj))
            .WithMessage("CNPJ deve conter apenas números.")
            .Length(14)
            .When(x => !string.IsNullOrWhiteSpace(x.Cnpj))
            .WithMessage("CNPJ deve conter 14 dígitos.")
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

        RuleFor(x => x.Endereco)
            .MaximumLength(255)
            .WithMessage("Endereço deve ter no máximo 255 caracteres.");

        RuleFor(x => x.Observacoes)
            .Must(x => InputNormalizer.SanitizeTextOrNull(x) is null || InputNormalizer.SanitizeTextOrNull(x)!.Length <= 500)
            .WithMessage("Observações deve ter no máximo 500 caracteres.");

        // ---------------------- Veiculos ----------------------
        RuleFor(x => x.Veiculos)
            .NotNull()
            .WithMessage("Informe ao menos um veículo para concluir o cadastro do cliente.")
            .Must(veiculos => veiculos is not null && veiculos.Count >= 1)
            .WithMessage("Informe ao menos um veículo para concluir o cadastro do cliente.");

        RuleForEach(x => x.Veiculos)
            .ChildRules(veiculo =>
            {
                veiculo.RuleFor(x => x.Placa)
                    .Cascade(CascadeMode.Stop)
                    .Must(placa => InputNormalizer.PlacaOrNull(placa) is not null)
                    .WithMessage("Placa do veículo é obrigatória.")
                    .Must(placa =>
                    {
                        string? placaNormalizada = InputNormalizer.PlacaOrNull(placa);
                        return placaNormalizada is not null
                            && Regex.IsMatch(placaNormalizada, @"^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$");
                    })
                    .WithMessage("Placa inválida. Formatos aceitos: AAA0000 ou AAA0A00.");

                veiculo.RuleFor(x => x.Modelo)
                    .Cascade(CascadeMode.Stop)
                    .Must(modelo =>
                    {
                        string? valor = InputNormalizer.TrimOrNull(modelo);
                        return valor is not null && valor.Length is >= 2 and <= 80;
                    })
                    .WithMessage("Modelo do veículo deve ter entre 2 e 80 caracteres.");

                veiculo.RuleFor(x => x.Fabricante)
                    .Cascade(CascadeMode.Stop)
                    .Must(fabricante =>
                    {
                        string? valor = InputNormalizer.TrimOrNull(fabricante);
                        return valor is not null && valor.Length is >= 2 and <= 80;
                    })
                    .WithMessage("Fabricante do veículo deve ter entre 2 e 80 caracteres.");

                veiculo.RuleFor(x => x.Cor)
                    .Cascade(CascadeMode.Stop)
                    .Must(cor =>
                    {
                        string? valor = InputNormalizer.TrimOrNull(cor);
                        return valor is not null && valor.Length is >= 2 and <= 40;
                    })
                    .WithMessage("Cor do veículo deve ter entre 2 e 40 caracteres.");
            });

        RuleFor(x => x.Veiculos)
            .Must(veiculos =>
            {
                if (veiculos is null || veiculos.Count == 0)
                {
                    return true;
                }
                List<string> placas = veiculos
                    .Select(v => InputNormalizer.PlacaOrNull(v.Placa))
                    .Where(placa => placa is not null)
                    .Select(placa => placa!)
                    .ToList();
                return placas.Count == placas.Distinct().Count();
            })
            .WithMessage("Existem placas duplicadas na lista de veículos informada.");
            }
}
