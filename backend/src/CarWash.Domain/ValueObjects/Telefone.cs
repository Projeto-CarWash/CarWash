using CarWash.Domain.Common;

namespace CarWash.Domain.ValueObjects;

/// <summary>
/// Telefone armazenado como dígitos puros (10 para fixo, 11 para móvel BR).
/// Tamanho máximo do schema é <c>VARCHAR(11)</c> — DB001 §01.
/// </summary>
public sealed record Telefone
{
    public string Valor { get; }

    public Telefone(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DomainException("Telefone não pode ser vazio.");
        }

        var apenasDigitos = new string([.. valor.Where(char.IsDigit)]);
        if (apenasDigitos.Length is < 10 or > 11)
        {
            throw new DomainException("Telefone deve possuir 10 ou 11 dígitos.");
        }

        Valor = apenasDigitos;
    }

    public override string ToString() => Valor;

    public static implicit operator string(Telefone telefone) =>
        telefone is null ? throw new ArgumentNullException(nameof(telefone)) : telefone.Valor;
}
