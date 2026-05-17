using CarWash.Domain.Common;

namespace CarWash.Domain.ValueObjects;

/// <summary>
/// Endereço estruturado (RF002). CEP em 8 dígitos sem máscara, UF em 2 letras
/// maiúsculas (validada contra lista oficial dos 27 estados brasileiros).
/// Complemento opcional.
/// </summary>
public sealed record Endereco
{
    private static readonly HashSet<string> UfsValidas = new(StringComparer.OrdinalIgnoreCase)
    {
        "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA",
        "MT", "MS", "MG", "PA", "PB", "PR", "PE", "PI", "RJ", "RN",
        "RS", "RO", "RR", "SC", "SP", "SE", "TO",
    };

    public string Cep { get; }

    public string Logradouro { get; }

    public string Numero { get; }

    public string? Complemento { get; }

    public string Bairro { get; }

    public string Cidade { get; }

    public string Uf { get; }

    public Endereco(
        string cep,
        string logradouro,
        string numero,
        string? complemento,
        string bairro,
        string cidade,
        string uf)
    {
        var cepDigits = ApenasDigitos(cep);
        if (cepDigits.Length != 8)
        {
            throw new DomainException("CEP deve possuir 8 dígitos.");
        }

        if (string.IsNullOrWhiteSpace(logradouro) || logradouro.Length > 150)
        {
            throw new DomainException("Logradouro é obrigatório e deve ter no máximo 150 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(numero) || numero.Length > 20)
        {
            throw new DomainException("Número é obrigatório e deve ter no máximo 20 caracteres.");
        }

        if (complemento is not null && complemento.Length > 100)
        {
            throw new DomainException("Complemento deve ter no máximo 100 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(bairro) || bairro.Length > 100)
        {
            throw new DomainException("Bairro é obrigatório e deve ter no máximo 100 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(cidade) || cidade.Length > 100)
        {
            throw new DomainException("Cidade é obrigatória e deve ter no máximo 100 caracteres.");
        }

        var ufNormalizada = (uf ?? string.Empty).Trim().ToUpperInvariant();
        if (!UfsValidas.Contains(ufNormalizada))
        {
            throw new DomainException("UF inválida.");
        }

        Cep = cepDigits;
        Logradouro = logradouro.Trim();
        Numero = numero.Trim();
        Complemento = string.IsNullOrWhiteSpace(complemento) ? null : complemento.Trim();
        Bairro = bairro.Trim();
        Cidade = cidade.Trim();
        Uf = ufNormalizada;
    }

    private static string ApenasDigitos(string? valor)
        => string.IsNullOrEmpty(valor) ? string.Empty : new string([.. valor.Where(char.IsDigit)]);
}
