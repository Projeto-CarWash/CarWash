using System.Text.RegularExpressions;
using CarWash.Domain.Common;

namespace CarWash.Domain.ValueObjects;

/// <summary>
/// Value object de email — sempre armazenado em <c>lowercase</c>, com validação mínima
/// de formato. Encapsula a normalização exigida em <c>usuarios.email</c> (DB001 §01.1).
/// </summary>
public sealed partial record Email
{
    private const int TamanhoMaximo = 150;

    public string Valor { get; }

    public Email(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DomainException("Email não pode ser vazio.");
        }

        var normalizado = valor.Trim().ToLowerInvariant();
        if (normalizado.Length > TamanhoMaximo)
        {
            throw new DomainException($"Email excede {TamanhoMaximo} caracteres.");
        }

        if (!FormatoRegex().IsMatch(normalizado))
        {
            throw new DomainException("Email inválido.");
        }

        Valor = normalizado;
    }

    public override string ToString() => Valor;

    public static implicit operator string(Email email) =>
        email is null ? throw new ArgumentNullException(nameof(email)) : email.Valor;

    [GeneratedRegex(@"^[a-z0-9._%+\-]+@[a-z0-9.\-]+\.[a-z]{2,}$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex FormatoRegex();
}
