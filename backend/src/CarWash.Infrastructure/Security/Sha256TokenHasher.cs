using System.Security.Cryptography;
using System.Text;
using CarWash.Application.Abstractions;

namespace CarWash.Infrastructure.Security;

/// <summary>
/// Hash de refresh token via SHA-256 (decisão P05). Comparação em tempo constante
/// com <see cref="CryptographicOperations.FixedTimeEquals(ReadOnlySpan{byte}, ReadOnlySpan{byte})"/>.
/// </summary>
public sealed class Sha256TokenHasher : ITokenHasher
{
    public string Hash(string tokenBruto)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenBruto);

        var bytes = Encoding.UTF8.GetBytes(tokenBruto);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public bool Verify(string tokenBruto, string hashArmazenado)
    {
        if (string.IsNullOrWhiteSpace(tokenBruto) || string.IsNullOrWhiteSpace(hashArmazenado))
        {
            return false;
        }

        var calculadoHex = Hash(tokenBruto);
        var calculado = Encoding.ASCII.GetBytes(calculadoHex);
        var armazenado = Encoding.ASCII.GetBytes(hashArmazenado.ToLowerInvariant());

        if (calculado.Length != armazenado.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(calculado, armazenado);
    }
}
