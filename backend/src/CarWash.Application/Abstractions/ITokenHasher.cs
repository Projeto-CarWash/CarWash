namespace CarWash.Application.Abstractions;

/// <summary>
/// Abstração para hash de refresh tokens — implementação default é SHA-256
/// com comparação em tempo constante (decisão P05).
/// </summary>
public interface ITokenHasher
{
    string Hash(string tokenBruto);

    bool Verify(string tokenBruto, string hashArmazenado);
}
