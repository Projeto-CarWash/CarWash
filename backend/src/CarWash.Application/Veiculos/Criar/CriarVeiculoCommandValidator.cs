using CarWash.Domain.Entities;
using FluentValidation;

namespace CarWash.Application.Veiculos.Criar;

/// <summary>
/// Validador sintático do RF005. RN003 (formato da placa) é defendida no value
/// object <c>Placa</c> + CHECK <c>ck_veiculos_placa_formato</c>; aqui validamos
/// presença/tamanho dos campos para falhar com 400 ProblemDetails estruturado
/// antes de chegar ao domínio.
/// </summary>
public sealed class CriarVeiculoCommandValidator : AbstractValidator<CriarVeiculoCommand>
{
    public CriarVeiculoCommandValidator()
    {
        RuleFor(x => x.ClienteId)
            .NotEmpty().WithMessage("Identificador do cliente é obrigatório.");

        RuleFor(x => x.Placa)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Placa é obrigatória.")
            .MaximumLength(10).WithMessage("Placa deve ter no máximo 10 caracteres.");

        RuleFor(x => x.Modelo)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Modelo é obrigatório.")
            .MaximumLength(80).WithMessage("Modelo deve ter no máximo 80 caracteres.");

        RuleFor(x => x.Fabricante)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Fabricante é obrigatório.")
            .MaximumLength(80).WithMessage("Fabricante deve ter no máximo 80 caracteres.");

        RuleFor(x => x.Cor)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Cor é obrigatória.")
            .MaximumLength(40).WithMessage("Cor deve ter no máximo 40 caracteres.");

        RuleFor(x => x.Ano)
            .InclusiveBetween(Veiculo.AnoMinimo, Veiculo.AnoMaximo)
            .When(x => x.Ano.HasValue)
            .WithMessage($"Ano deve estar entre {Veiculo.AnoMinimo} e {Veiculo.AnoMaximo}.");
    }
}
