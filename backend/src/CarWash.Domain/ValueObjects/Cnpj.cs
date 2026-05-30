using CarWash.Domain.Common;

namespace CarWash.Domain.ValueObjects;

/// <summary>
/// CNPJ persistido como 14 dígitos sem máscara, com validação dos dígitos verificadores.
/// </summary>
public sealed record Cnpj
{
    private static readonly int[] PesosPrimeiroDigito = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] PesosSegundoDigito = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    public string Valor { get; }

    public Cnpj(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DomainException("CNPJ não pode ser vazio.");
        }

        string apenasDigitos = new string([.. valor.Where(char.IsDigit)]);
        if (apenasDigitos.Length != 14)
        {
            throw new DomainException("CNPJ deve possuir 14 dígitos.");
        }

        if (!DigitosValidos(apenasDigitos))
        {
            throw new DomainException("CNPJ inválido.");
        }

        Valor = apenasDigitos;
    }

    /// <inheritdoc/>
    public override string ToString() => Valor;

    public static implicit operator string(Cnpj cnpj) =>
        cnpj is null ? throw new ArgumentNullException(nameof(cnpj)) : cnpj.Valor;

    private static bool DigitosValidos(string cnpj)
    {
        if (cnpj.Distinct().Count() == 1)
        {
            return false;
        }

        int digito1 = CalcularDigito(cnpj, PesosPrimeiroDigito);
        int digito2 = CalcularDigito(cnpj, PesosSegundoDigito);

        return cnpj[12] - '0' == digito1 && cnpj[13] - '0' == digito2;
    }

    private static int CalcularDigito(string cnpj, int[] pesos)
    {
        int soma = 0;
        for (int i = 0; i < pesos.Length; i++)
        {
            soma += (cnpj[i] - '0') * pesos[i];
        }

        int resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
