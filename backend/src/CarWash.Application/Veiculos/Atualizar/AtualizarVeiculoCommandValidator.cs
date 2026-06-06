using System.Text.RegularExpressions;
using CarWash.Domain.Entities;
using FluentValidation;

namespace CarWash.Application.Veiculos.Atualizar;

/// <summary>
/// Validador do PUT de veículo. Mesmas regras de <see cref="Criar.CriarVeiculoCommandValidator"/>,
/// exceto <c>ClienteId</c> (vem da rota, não do body).
/// </summary>
public sealed class AtualizarVeiculoCommandValidator : AbstractValidator<AtualizarVeiculoCommand>
{
    private static readonly Regex PlacaFormatoRegex = new(
        @"^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(200));

    public AtualizarVeiculoCommandValidator()
    {
        RuleFor(x => x.VeiculoId)
            .NotEqual(Guid.Empty).WithMessage("Identificador do veículo é obrigatório.");

        RuleFor(x => x.ClienteId)
            .NotEqual(Guid.Empty).WithMessage("Identificador do cliente é obrigatório.");

        RuleFor(x => x.Placa)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("O campo placa é obrigatório.")
            .Must(PlacaComFormatoValido).WithMessage("A placa informada não está em um formato válido.");

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

    private static bool PlacaComFormatoValido(string? placa)
    {
        if (string.IsNullOrWhiteSpace(placa))
        {
            return false;
        }

        string normalizado = placa.Trim().ToUpperInvariant();

        return normalizado.Length == 7 && PlacaFormatoRegex.IsMatch(normalizado);
    }
}
