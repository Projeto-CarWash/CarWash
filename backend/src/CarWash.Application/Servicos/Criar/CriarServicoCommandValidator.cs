using CarWash.Application.Common;
using CarWash.Domain.Entities;
using FluentValidation;

namespace CarWash.Application.Servicos.Criar;

/// <summary>
/// Validador do RF006. Nome obrigatório (3-120 chars), preço positivo,
/// duração entre 1 e 1440 minutos.
/// </summary>
public sealed class CriarServicoCommandValidator : AbstractValidator<CriarServicoCommand>
{
    public CriarServicoCommandValidator()
    {
        RuleFor(x => x.Nome)
            .Cascade(CascadeMode.Stop)
            .Must(nome => !string.IsNullOrWhiteSpace(InputNormalizer.SanitizeTextOrNull(nome)))
            .WithMessage("O nome do serviço é obrigatório.")
            .Must(nome => InputNormalizer.SanitizeTextOrNull(nome)!.Length >= Servico.NomeMinLength)
            .WithMessage($"O nome do serviço deve ter no mínimo {Servico.NomeMinLength} caracteres.")
            .Must(nome => InputNormalizer.SanitizeTextOrNull(nome)!.Length <= Servico.NomeMaxLength)
            .WithMessage($"O nome do serviço deve ter no máximo {Servico.NomeMaxLength} caracteres.");

        RuleFor(x => x.Preco)
            .NotNull().WithMessage("O preço do serviço é obrigatório.")
            .GreaterThan(0m).WithMessage("O preço do serviço deve ser maior que zero.");

        RuleFor(x => x.DuracaoMin)
            .NotNull().WithMessage("A duração do serviço é obrigatória.")
            .GreaterThan(0).WithMessage("A duração do serviço deve ser maior que zero.")
            .LessThanOrEqualTo(Servico.DuracaoMinValorMax)
            .WithMessage($"A duração do serviço não pode ultrapassar {Servico.DuracaoMinValorMax} minutos.");
    }
}
