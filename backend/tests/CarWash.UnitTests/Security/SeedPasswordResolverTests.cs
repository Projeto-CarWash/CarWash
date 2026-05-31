using CarWash.Infrastructure.Security;
using FluentAssertions;
using Xunit;

namespace CarWash.UnitTests.Security;

public class SeedPasswordResolverTests
{
    [Fact]
    public void Lanca_InvalidOperationException_quando_env_ausente()
    {
        string? anterior = Environment.GetEnvironmentVariable(SeedPasswordResolver.EnvironmentVariableName);
        try
        {
            Environment.SetEnvironmentVariable(SeedPasswordResolver.EnvironmentVariableName, null);
            var act = SeedPasswordResolver.ResolveAdminArgon2idHash;
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*CARWASH_SEED_ADMIN_PASSWORD não definida*");
        }
        finally
        {
            Environment.SetEnvironmentVariable(SeedPasswordResolver.EnvironmentVariableName, anterior);
        }
    }

    [Fact]
    public void Lanca_InvalidOperationException_quando_env_vazia()
    {
        string? anterior = Environment.GetEnvironmentVariable(SeedPasswordResolver.EnvironmentVariableName);
        try
        {
            Environment.SetEnvironmentVariable(SeedPasswordResolver.EnvironmentVariableName, "   ");
            var act = SeedPasswordResolver.ResolveAdminArgon2idHash;
            act.Should().Throw<InvalidOperationException>();
        }
        finally
        {
            Environment.SetEnvironmentVariable(SeedPasswordResolver.EnvironmentVariableName, anterior);
        }
    }

    [Fact]
    public void Gera_hash_argon2id_PHC_quando_env_definida()
    {
        string? anterior = Environment.GetEnvironmentVariable(SeedPasswordResolver.EnvironmentVariableName);
        try
        {
            Environment.SetEnvironmentVariable(SeedPasswordResolver.EnvironmentVariableName, "SenhaTeste!2026");
            string hash = SeedPasswordResolver.ResolveAdminArgon2idHash();
            hash.Should().StartWith("$argon2id$v=19$m=65536,t=3,p=1$");
        }
        finally
        {
            Environment.SetEnvironmentVariable(SeedPasswordResolver.EnvironmentVariableName, anterior);
        }
    }
}
