using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CarWash.Application.Abstractions;
using Konscious.Security.Cryptography;

namespace CarWash.Infrastructure.Security;

/// <summary>
/// Implementação do hash de senha em Argon2id (ADR 0002) — formato PHC canônico.
/// Parâmetros iniciais: <c>m=65536 KiB (64 MiB)</c>, <c>t=3</c>, <c>p=1</c>, salt 16 bytes.
/// </summary>
#pragma warning disable S101 // "Argon2id" é o nome canônico da variante (RFC 9106); manter a grafia.
public sealed class Argon2idPasswordHasher : IPasswordHasher
#pragma warning restore S101
{
    public const int MemoryKiB = 65536;
    public const int Iterations = 3;
    public const int Parallelism = 1;
    public const int SaltLengthBytes = 16;
    public const int HashLengthBytes = 32;
    private const int Argon2Version = 19;

    /// <inheritdoc/>
    public string Hash(string senha)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(senha);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltLengthBytes);
        byte[] hash = ComputeHash(senha, salt, MemoryKiB, Iterations, Parallelism, HashLengthBytes);

        return string.Format(
            CultureInfo.InvariantCulture,
            "$argon2id$v={0}$m={1},t={2},p={3}${4}${5}",
            Argon2Version,
            MemoryKiB,
            Iterations,
            Parallelism,
            Convert.ToBase64String(salt).TrimEnd('='),
            Convert.ToBase64String(hash).TrimEnd('='));
    }

    /// <inheritdoc/>
    public bool Verify(string senha, string hash)
    {
        if (string.IsNullOrWhiteSpace(senha) || string.IsNullOrWhiteSpace(hash))
        {
            return false;
        }

        if (!TryParse(hash, out var parametros))
        {
            return false;
        }

        byte[] calculado = ComputeHash(
            senha,
            parametros.Salt,
            parametros.MemoryKiB,
            parametros.Iterations,
            parametros.Parallelism,
            parametros.HashEsperado.Length);

        return CryptographicOperations.FixedTimeEquals(calculado, parametros.HashEsperado);
    }

    /// <inheritdoc/>
    public bool NeedsRehash(string hash)
    {
        if (!TryParse(hash, out var parametros))
        {
            // hash em formato desconhecido → rotacionar.
            return true;
        }

        return parametros.MemoryKiB < MemoryKiB
            || parametros.Iterations < Iterations
            || parametros.Parallelism < Parallelism;
    }

    private static byte[] ComputeHash(
        string senha,
        byte[] salt,
        int memoryKiB,
        int iterations,
        int parallelism,
        int outputBytes)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(senha))
        {
            Salt = salt,
            DegreeOfParallelism = parallelism,
            Iterations = iterations,
            MemorySize = memoryKiB,
        };

        return argon2.GetBytes(outputBytes);
    }

    private static bool TryParse(string hash, out PhcArgon2id parametros)
    {
        parametros = default;
        if (string.IsNullOrWhiteSpace(hash))
        {
            return false;
        }

        string[] partes = hash.Split('$', StringSplitOptions.None);

        // formato: ["", "argon2id", "v=N", "m=N,t=N,p=N", "<salt>", "<hash>"]
        if (partes.Length != 6 || partes[1] != "argon2id")
        {
            return false;
        }

        if (!partes[2].StartsWith("v=", StringComparison.Ordinal)
            || !int.TryParse(partes[2][2..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int versao))
        {
            return false;
        }

        if (versao != Argon2Version)
        {
            return false;
        }

        string[] paramsTokens = partes[3].Split(',', StringSplitOptions.None);
        if (paramsTokens.Length != 3)
        {
            return false;
        }

        if (!TryParseTokenInt(paramsTokens[0], "m=", out int memoryKiB)
            || !TryParseTokenInt(paramsTokens[1], "t=", out int iterations)
            || !TryParseTokenInt(paramsTokens[2], "p=", out int parallelism))
        {
            return false;
        }

        if (!TryDecodeBase64(partes[4], out byte[]? salt)
            || !TryDecodeBase64(partes[5], out byte[]? hashEsperado))
        {
            return false;
        }

        parametros = new PhcArgon2id(memoryKiB, iterations, parallelism, salt, hashEsperado);
        return true;
    }

    private static bool TryParseTokenInt(string token, string prefix, out int valor)
    {
        valor = 0;
        if (!token.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        return int.TryParse(token[prefix.Length..], NumberStyles.Integer, CultureInfo.InvariantCulture, out valor);
    }

    private static bool TryDecodeBase64(string raw, out byte[] decoded)
    {
        decoded = [];
        try
        {
            string padded = raw + new string('=', (4 - (raw.Length % 4)) % 4);
            decoded = Convert.FromBase64String(padded);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

#pragma warning disable S101, S3218 // "Argon2id" é o nome canônico (RFC 9106); intencional o shadowing dos params.
    private readonly record struct PhcArgon2id(int MemoryKiB, int Iterations, int Parallelism, byte[] Salt, byte[] HashEsperado);
#pragma warning restore S101, S3218
}
