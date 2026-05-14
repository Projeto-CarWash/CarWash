using CarWash.Domain.Common;

namespace CarWash.Domain.ValueObjects;

/// <summary>
/// CPF persistido como 11 dígitos sem máscara. Valida dígitos verificadores
/// (não basta o tamanho — protege a integridade dos uniques parciais).
/// </summary>
public sealed record Cpf
{
    public string Valor { get; }

    public Cpf(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DomainException("CPF não pode ser vazio.");
        }

        var apenasDigitos = new string([.. valor.Where(char.IsDigit)]);
        if (apenasDigitos.Length != 11)
        {
            throw new DomainException("CPF deve possuir 11 dígitos.");
        }

        if (!DigitosValidos(apenasDigitos))
        {
            throw new DomainException("CPF inválido.");
        }

        Valor = apenasDigitos;
    }

    public override string ToString() => Valor;

    public static implicit operator string(Cpf cpf) =>
        cpf is null ? throw new ArgumentNullException(nameof(cpf)) : cpf.Valor;

    private static bool DigitosValidos(string cpf)
    {
        if (cpf.Distinct().Count() == 1)
        {
            return false; // bloqueia 000..000, 111..111, etc.
        }

        var digito1 = CalcularDigito(cpf, 9, 10);
        var digito2 = CalcularDigito(cpf, 10, 11);

        return cpf[9] - '0' == digito1 && cpf[10] - '0' == digito2;
    }

    private static int CalcularDigito(string cpf, int tamanho, int pesoInicial)
    {
        var soma = 0;
        for (var i = 0; i < tamanho; i++)
        {
            soma += (cpf[i] - '0') * (pesoInicial - i);
        }

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
