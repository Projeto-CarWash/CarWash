using CarWash.Application.Common;
using CarWash.Domain.Entities;
using FluentValidation;

namespace CarWash.Application.Servicos.Atualizar;

/// <summary>
/// Mesmas regras de <see cref="Criar.CriarServicoCommandValidator"/>.
/// </summary>
public sealed class AtualizarServicoCommandValidator : AbstractValidator<AtualizarServicoCommand>
{
    public AtualizarServicoCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty).WithMessage("Identificador do serviço é obrigatório.");

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
