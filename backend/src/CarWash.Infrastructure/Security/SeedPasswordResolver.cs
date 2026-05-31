namespace CarWash.Infrastructure.Security;

/// <summary>
/// Resolve a senha do administrador inicial a partir da variável de ambiente
/// obrigatória <c>CARWASH_SEED_ADMIN_PASSWORD</c> e devolve o hash Argon2id (PHC).
///
/// Sem fallback (decisão P12). Se a variável não estiver presente ou estiver vazia,
/// a migration falha de forma explícita — garantindo que nenhum ambiente sobe
/// com senha hardcoded ou aleatória oculta.
/// </summary>
public static class SeedPasswordResolver
{
    public const string EnvironmentVariableName = "CARWASH_SEED_ADMIN_PASSWORD";

    /// <summary>
    /// Lê a senha em texto puro da env e devolve o hash Argon2id PHC.
    /// Lança <see cref="InvalidOperationException"/> se a variável estiver ausente.
    /// </summary>
    /// <returns></returns>
    public static string ResolveAdminArgon2idHash()
    {
        string? senha = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (string.IsNullOrWhiteSpace(senha))
        {
            throw new InvalidOperationException(
                $"{EnvironmentVariableName} não definida. Seed do administrador inicial requer essa "
                + "variável para gerar o hash Argon2id. Defina-a antes de rodar `dotnet ef database update` "
                + "(em dev: export; em hom/prod: secret manager).");
        }

        var hasher = new Argon2idPasswordHasher();
        return hasher.Hash(senha);
    }
}
