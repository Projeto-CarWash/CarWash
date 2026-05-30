using System.Text.RegularExpressions;
using CarWash.Domain.Common;

namespace CarWash.Domain.ValueObjects;

/// <summary>
/// Value object de placa veicular — sempre armazenada em uppercase, sem espaços.
/// Aceita formato Mercosul (<c>AAA9A99</c>) e formato antigo (<c>AAA9999</c>).
/// Normalização exigida pelo unique <c>uk_veiculos_placa</c> (RN003, DB001 §02).
/// </summary>
public sealed partial record Placa
{
    private const int TamanhoMaximo = 10;

    public string Valor { get; }

    public Placa(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DomainException("Placa não pode ser vazia.");
        }

        var normalizado = valor.Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();

        if (normalizado.Length > TamanhoMaximo)
        {
            throw new DomainException($"Placa excede {TamanhoMaximo} caracteres.");
        }

        if (!FormatoRegex().IsMatch(normalizado))
        {
            throw new DomainException("Placa inválida: utilize letras e números (formato antigo AAA9999 ou Mercosul AAA9A99).");
        }

        Valor = normalizado;
    }

    public override string ToString() => Valor;

    public static implicit operator string(Placa placa) =>
        placa is null ? throw new ArgumentNullException(nameof(placa)) : placa.Valor;

    [GeneratedRegex(@"^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex FormatoRegex();
}
