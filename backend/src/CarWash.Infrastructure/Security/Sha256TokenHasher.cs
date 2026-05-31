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
    /// <inheritdoc/>
    public string Hash(string tokenBruto)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenBruto);

        byte[] bytes = Encoding.UTF8.GetBytes(tokenBruto);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <inheritdoc/>
    public bool Verify(string tokenBruto, string hashArmazenado)
    {
        if (string.IsNullOrWhiteSpace(tokenBruto) || string.IsNullOrWhiteSpace(hashArmazenado))
        {
            return false;
        }

        string calculadoHex = Hash(tokenBruto);
        byte[] calculado = Encoding.ASCII.GetBytes(calculadoHex);
        byte[] armazenado = Encoding.ASCII.GetBytes(hashArmazenado.ToLowerInvariant());

        if (calculado.Length != armazenado.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(calculado, armazenado);
    }
}
