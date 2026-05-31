using CarWash.Application.Common;
using CarWash.Domain.Entities;
using FluentValidation;

namespace CarWash.Application.Clientes.Atualizar;

/// <summary>
/// Mesmas regras de <see cref="Criar.CriarClienteCommandValidator"/>, exceto que
/// CPF/CNPJ não são editáveis (mantidos na entidade).
/// </summary>
public sealed class AtualizarClienteCommandValidator : AbstractValidator<AtualizarClienteCommand>
{
    public AtualizarClienteCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty).WithMessage("Identificador do cliente é obrigatório.");

        RuleFor(x => x.Nome)
            .Cascade(CascadeMode.Stop)
            .Must(nome => !string.IsNullOrWhiteSpace(InputNormalizer.SanitizeTextOrNull(nome)))
            .WithMessage("O nome é obrigatório.")
            .Must(nome => InputNormalizer.SanitizeTextOrNull(nome)!.Length >= 3)
            .WithMessage("O nome deve ter no mínimo 3 caracteres.")
            .Must(nome => InputNormalizer.SanitizeTextOrNull(nome)!.Length <= 100)
            .WithMessage("O nome deve ter no máximo 100 caracteres.");

        RuleFor(x => x.DataNascimento)
            .NotNull().WithMessage("Data de nascimento é obrigatória.")
            .Must(d => IdadeEntreLimites(d!.Value))
            .When(x => x.DataNascimento.HasValue)
            .WithMessage($"Cliente deve ter entre {Cliente.IdadeMinima} e {Cliente.IdadeMaxima} anos.");

        RuleFor(x => x.Celular)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Celular é obrigatório.")
            .Must(InputNormalizer.ContainsOnlyDigits)
            .WithMessage("Celular deve conter apenas números.")
            .Must(x => InputNormalizer.OnlyDigitsOrNull(x)?.Length == 11)
            .WithMessage("Celular deve conter 11 dígitos.");

        RuleFor(x => x.Telefone)
            .Cascade(CascadeMode.Stop)
            .Must(InputNormalizer.ContainsOnlyDigits)
            .When(x => !string.IsNullOrWhiteSpace(x.Telefone))
            .WithMessage("Telefone deve conter apenas números.")
            .Must(x => InputNormalizer.OnlyDigitsOrNull(x) is null
                || InputNormalizer.OnlyDigitsOrNull(x)!.Length is >= 10 and <= 11)
            .WithMessage("Telefone deve conter entre 10 e 11 dígitos.");

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .MinimumLength(5).When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("E-mail deve ter no mínimo 5 caracteres.")
            .MaximumLength(150).When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("E-mail deve ter no máximo 150 caracteres.")
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("E-mail inválido.");

        RuleFor(x => x.Endereco)
            .NotNull().WithMessage("Endereço é obrigatório.");

        When(x => x.Endereco is not null, () =>
        {
            RuleFor(x => x.Endereco!.Cep)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("CEP é obrigatório.")
                .Must(InputNormalizer.ContainsOnlyDigits).WithMessage("CEP deve conter apenas números.")
                .Length(8).WithMessage("CEP deve conter 8 dígitos.");

            RuleFor(x => x.Endereco!.Logradouro)
                .NotEmpty().WithMessage("Logradouro é obrigatório.")
                .MaximumLength(150);

            RuleFor(x => x.Endereco!.Numero)
                .NotEmpty().WithMessage("Número é obrigatório.")
                .MaximumLength(20);

            RuleFor(x => x.Endereco!.Complemento)
                .MaximumLength(100)
                .When(x => !string.IsNullOrWhiteSpace(x.Endereco!.Complemento));

            RuleFor(x => x.Endereco!.Bairro)
                .NotEmpty().WithMessage("Bairro é obrigatório.")
                .MaximumLength(100);

            RuleFor(x => x.Endereco!.Cidade)
                .NotEmpty().WithMessage("Cidade é obrigatória.")
                .MaximumLength(100);

            RuleFor(x => x.Endereco!.Uf)
                .NotEmpty().WithMessage("UF é obrigatória.")
                .Length(2);
        });
    }

    private static bool IdadeEntreLimites(DateOnly dataNascimento)
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        if (dataNascimento > hoje)
        {
            return false;
        }

        int idade = hoje.Year - dataNascimento.Year;
        if (dataNascimento > hoje.AddYears(-idade))
        {
            idade--;
        }

        return idade >= Cliente.IdadeMinima && idade <= Cliente.IdadeMaxima;
    }
}
