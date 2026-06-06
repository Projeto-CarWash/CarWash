using CarWash.Application.Common;
using CarWash.Domain.Entities;
using FluentValidation;

namespace CarWash.Application.Clientes.Criar;

/// <summary>
/// Validador do RF002 + RF003. Endereço estruturado, celular obrigatório,
/// data de nascimento com idade entre <see cref="Cliente.IdadeMinima"/> e
/// <see cref="Cliente.IdadeMaxima"/>, CPF/CNPJ exclusivos.
/// </summary>
public sealed class CriarClienteCommandValidator : AbstractValidator<CriarClienteCommand>
{
    public CriarClienteCommandValidator()
    {
        RuleFor(x => x.Nome)
            .Cascade(CascadeMode.Stop)
            .Must(nome => !string.IsNullOrWhiteSpace(InputNormalizer.SanitizeTextOrNull(nome)))
            .WithMessage("O nome é obrigatório.")
            .Must(nome => InputNormalizer.SanitizeTextOrNull(nome)!.Length >= 3)
            .WithMessage("O nome deve ter no mínimo 3 caracteres.")
            .Must(nome => InputNormalizer.SanitizeTextOrNull(nome)!.Length <= 100)
            .WithMessage("O nome deve ter no máximo 100 caracteres.")
            .Matches(@"^[a-zA-ZáàãâäéèêëíïóôõöúüçñÁÀÃÂÄÉÈÊËÍÏÓÔÕÖÚÜÇÑ\s\-']+$")
            .WithMessage("Nome não deve conter números ou caracteres especiais.");

        RuleFor(x => x.DataNascimento)
            .NotNull().WithMessage("Data de nascimento é obrigatória.")
            .Must(d => IdadeEntreLimites(d!.Value))
            .When(x => x.DataNascimento.HasValue)
            .WithMessage($"Cliente deve ter entre {Cliente.IdadeMinima} e {Cliente.IdadeMaxima} anos.");

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
            .MinimumLength(5)
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("E-mail deve ter no mínimo 5 caracteres.")
            .MaximumLength(150)
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("E-mail deve ter no máximo 150 caracteres.")
            .Must(EmailValido)
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
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
                .MaximumLength(150).WithMessage("Logradouro deve ter no máximo 150 caracteres.")
                .Matches(@"^[a-zA-ZáàãâäéèêëíïóôõöúüçñÁÀÃÂÄÉÈÊËÍÏÓÔÕÖÚÜÇÑ0-9\s.,\-]+$")
                .WithMessage("Logradouro não deve conter caracteres especiais.");

            RuleFor(x => x.Endereco!.Numero)
                .NotEmpty().WithMessage("Número é obrigatório.")
                .MaximumLength(20).WithMessage("Número deve ter no máximo 20 caracteres.");

            RuleFor(x => x.Endereco!.Complemento)
                .MaximumLength(100)
                .When(x => !string.IsNullOrWhiteSpace(x.Endereco!.Complemento))
                .WithMessage("Complemento deve ter no máximo 100 caracteres.");

            RuleFor(x => x.Endereco!.Bairro)
                .NotEmpty().WithMessage("Bairro é obrigatório.")
                .MaximumLength(100).WithMessage("Bairro deve ter no máximo 100 caracteres.")
                .Matches(@"^[a-zA-ZáàãâäéèêëíïóôõöúüçñÁÀÃÂÄÉÈÊËÍÏÓÔÕÖÚÜÇÑ0-9\s\-]+$")
                .WithMessage("Bairro não deve conter caracteres especiais.");

            RuleFor(x => x.Endereco!.Cidade)
                .NotEmpty().WithMessage("Cidade é obrigatória.")
                .MaximumLength(100).WithMessage("Cidade deve ter no máximo 100 caracteres.")
                .Matches(@"^[a-zA-ZáàãâäéèêëíïóôõöúüçñÁÀÃÂÄÉÈÊËÍÏÓÔÕÖÚÜÇÑ\s\-]+$")
                .WithMessage("Cidade não deve conter números ou caracteres especiais.");

            RuleFor(x => x.Endereco!.Uf)
                .NotEmpty().WithMessage("UF é obrigatória.")
                .Length(2).WithMessage("UF deve ter exatamente 2 caracteres.");
        });

        RuleFor(x => x.Observacoes)
            .MaximumLength(500)
            .WithMessage("Observações deve ter no máximo 500 caracteres.");
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

    private static bool EmailValido(string? email)
    {
        string? normalizado = InputNormalizer.EmailOrNull(email);

        if (string.IsNullOrWhiteSpace(normalizado))
        {
            return true;
        }

        if (normalizado.Length is < 5 or > 150)
        {
            return false;
        }

        var regex = new System.Text.RegularExpressions.Regex(@"^[^@\s]{1,64}@(?=.{1,253}$)(?!.*\.\.)[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?(?:\.[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?)+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (!regex.IsMatch(normalizado))
        {
            return false;
        }

        string dominio = normalizado.Split('@')[1];
        string[] partesDominio = dominio.Split('.');

        if (partesDominio.Length < 2)
        {
            return false;
        }

        if (partesDominio.Any(parte => string.IsNullOrWhiteSpace(parte)))
        {
            return false;
        }

        string tld = partesDominio[^1];

        if (!System.Text.RegularExpressions.Regex.IsMatch(tld, @"^[a-zA-Z]{2,24}$"))
        {
            return false;
        }

        for (int i = 0; i < partesDominio.Length - 1; i++)
        {
            if (string.Equals(partesDominio[i], partesDominio[i + 1], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
