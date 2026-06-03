using System.Text.RegularExpressions;
using CarWash.Domain.Entities;
using FluentValidation;

namespace CarWash.Application.Veiculos.CriarBatch;

/// <summary>
/// Validador do comando de batch RF005. Valida cada item individualmente
/// (presença + formato da placa) e verifica duplicidade de placas dentro
/// do payload antes de qualquer persistência.
/// </summary>
public sealed class CriarVeiculosBatchCommandValidator : AbstractValidator<CriarVeiculosBatchCommand>
{
    private static readonly Regex PlacaFormatoRegex = new(
        @"^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(200));

    public CriarVeiculosBatchCommandValidator()
    {
        RuleFor(x => x.ClienteId)
            .NotEmpty().WithMessage("Identificador do cliente é obrigatório.");

        RuleFor(x => x.Veiculos)
            .NotEmpty().WithMessage("O payload deve conter ao menos um veículo.");

        RuleForEach(x => x.Veiculos)
            .ChildRules(item =>
            {
                item.RuleFor(x => x.Placa)
                    .Cascade(CascadeMode.Stop)
                    .NotEmpty().WithMessage("O campo placa é obrigatório.")
                    .Must(PlacaComFormatoValido).WithMessage("A placa informada não está em um formato válido.");

                item.RuleFor(x => x.Modelo)
                    .Cascade(CascadeMode.Stop)
                    .NotEmpty().WithMessage("Modelo é obrigatório.")
                    .MaximumLength(80).WithMessage("Modelo deve ter no máximo 80 caracteres.");

                item.RuleFor(x => x.Fabricante)
                    .Cascade(CascadeMode.Stop)
                    .NotEmpty().WithMessage("Fabricante é obrigatório.")
                    .MaximumLength(80).WithMessage("Fabricante deve ter no máximo 80 caracteres.");

                item.RuleFor(x => x.Cor)
                    .Cascade(CascadeMode.Stop)
                    .NotEmpty().WithMessage("Cor é obrigatória.")
                    .MaximumLength(40).WithMessage("Cor deve ter no máximo 40 caracteres.");

                item.RuleFor(x => x.Ano)
                    .InclusiveBetween(Veiculo.AnoMinimo, Veiculo.AnoMaximo)
                    .When(x => x.Ano.HasValue)
                    .WithMessage($"Ano deve estar entre {Veiculo.AnoMinimo} e {Veiculo.AnoMaximo}.");
            });

        RuleFor(x => x.Veiculos)
            .Must(SemPlacasDuplicadasNoPayload).WithMessage("O payload contém placas duplicadas.")
            .When(x => x.Veiculos is { Count: > 0 });
    }

    private static bool PlacaComFormatoValido(string? placa)
    {
        if (string.IsNullOrWhiteSpace(placa))
        {
            return false;
        }

        string normalizado = placa.Trim().ToUpperInvariant();

        return normalizado.Length == 7 && PlacaFormatoRegex.IsMatch(normalizado);
    }

    private static bool SemPlacasDuplicadasNoPayload(IReadOnlyList<VeiculoItemCommand> veiculos)
    {
        if (veiculos is null or { Count: 0 })
        {
            return true;
        }

        var placasNormalizadas = veiculos
            .Where(v => !string.IsNullOrWhiteSpace(v.Placa))
            .Select(v => v.Placa!.Trim().ToUpperInvariant())
            .ToList();

        return placasNormalizadas.Count == placasNormalizadas.Distinct().Count();
    }
}
