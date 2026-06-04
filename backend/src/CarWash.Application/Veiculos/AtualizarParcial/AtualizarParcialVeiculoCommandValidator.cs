using System.Text.RegularExpressions;
using FluentValidation;

namespace CarWash.Application.Veiculos.AtualizarParcial;

/// <summary>
/// Validador do PATCH de veículo. Pelo menos 1 campo deve estar presente.
/// Se placa for enviada: trim + uppercase, validar formato Mercosul/antigo.
/// Hifens/espaços rejeitados — placa deve ser corrida (ex: ABC1D23).
/// </summary>
public sealed class AtualizarParcialVeiculoCommandValidator : AbstractValidator<AtualizarParcialVeiculoCommand>
{
    private static readonly Regex PlacaFormatoRegex = new(
        @"^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(200));

    public AtualizarParcialVeiculoCommandValidator()
    {
        RuleFor(x => x.VeiculoId)
            .NotEqual(Guid.Empty).WithMessage("Identificador do veículo é obrigatório.");

        RuleFor(x => x.ClienteId)
            .NotEqual(Guid.Empty).WithMessage("Identificador do cliente é obrigatório.");

        RuleFor(x => x.Placa)
            .Must(PlacaComFormatoValido)
            .When(x => !string.IsNullOrWhiteSpace(x.Placa))
            .WithMessage("A placa informada não está em um formato válido.");

        RuleFor(x => x.Modelo)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Modelo não pode ser vazio.")
            .MaximumLength(80).WithMessage("Modelo deve ter no máximo 80 caracteres.")
            .When(x => x.Modelo is not null);

        RuleFor(x => x.Fabricante)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Fabricante não pode ser vazio.")
            .MaximumLength(80).WithMessage("Fabricante deve ter no máximo 80 caracteres.")
            .When(x => x.Fabricante is not null);

        RuleFor(x => x.Cor)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Cor não pode ser vazia.")
            .MaximumLength(40).WithMessage("Cor deve ter no máximo 40 caracteres.")
            .When(x => x.Cor is not null);

        RuleFor(x => x)
            .Must(PeloMenosUmCampo)
            .WithMessage("Pelo menos um campo deve ser informado para atualização.");
    }

    private static bool PeloMenosUmCampo(AtualizarParcialVeiculoCommand command)
    {
        return !string.IsNullOrWhiteSpace(command.Placa)
            || !string.IsNullOrWhiteSpace(command.Modelo)
            || !string.IsNullOrWhiteSpace(command.Fabricante)
            || !string.IsNullOrWhiteSpace(command.Cor);
    }

    private static bool PlacaComFormatoValido(string? placa)
    {
        if (string.IsNullOrWhiteSpace(placa))
        {
            return false;
        }

        var normalizado = placa.Trim().ToUpperInvariant();

        return normalizado.Length == 7 && PlacaFormatoRegex.IsMatch(normalizado);
    }
}
