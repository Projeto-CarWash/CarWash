using System.Text.RegularExpressions;
using CarWash.Domain.Common;

namespace CarWash.Domain.ValueObjects;

/// <summary>
/// Value object de placa veicular — sempre armazenada em uppercase, sem espaços.
/// Aceita formato Mercosul (<c>AAA9A99</c>) e formato antigo (<c>AAA9999</c>).
/// Normalização (trim + uppercase) aplicada antes da validação (RF005).
/// Caracteres especiais (hifens, símbolos) NÃO são removidos — rejeitam a placa.
/// </summary>
public sealed partial record Placa
{
    private const int TamanhoEsperado = 7;

    public string Valor { get; }

    public Placa(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DomainException("O campo placa é obrigatório.");
        }

        var normalizado = valor.Trim().ToUpperInvariant();

        if (normalizado.Length != TamanhoEsperado)
        {
            throw new DomainException("A placa informada não está em um formato válido.");
        }

        if (!FormatoRegex().IsMatch(normalizado))
        {
            throw new DomainException("A placa informada não está em um formato válido.");
        }

        Valor = normalizado;
    }

    /// <inheritdoc/>
    public override string ToString() => Valor;

    public static implicit operator string(Placa placa) =>
        placa is null ? throw new ArgumentNullException(nameof(placa)) : placa.Valor;

    [GeneratedRegex(@"^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex FormatoRegex();
}
