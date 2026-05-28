using FluentValidation;
using System.Text.RegularExpressions;

namespace CarWash.Application.Servicos.Criar;

public sealed class CriarServicoCommandValidator : AbstractValidator<CriarServicoCommand>
{
    private static readonly Regex NomeRegex = new(
        @"^[a-zA-ZáàãâäéèêëíïóôõöúüçñÁÀÃÂÄÉÈÊËÍÏÓÔÕÖÚÜÇÑ0-9\s\-]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public CriarServicoCommandValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome do serviço é obrigatório.")
            .MaximumLength(120).WithMessage("Nome do serviço deve ter no máximo 120 caracteres.")
            .Matches(NomeRegex).WithMessage("Nome do serviço contém caracteres especiais inválidos.");

        RuleFor(x => x.Preco)
            .NotNull().WithMessage("Preço do serviço é obrigatório.")
            .GreaterThan(0m).WithMessage("Preço do serviço deve ser maior que zero.");

        RuleFor(x => x.DuracaoMin)
            .NotNull().WithMessage("Duração do serviço é obrigatória.")
            .GreaterThan(0).WithMessage("Duração do serviço deve ser maior que zero.");
    }
}
