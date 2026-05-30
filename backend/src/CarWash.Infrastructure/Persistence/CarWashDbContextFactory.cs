using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CarWash.Infrastructure.Persistence;

/// <summary>
/// Design-time factory para <c>dotnet ef</c>. Procura a conexão, em ordem:
/// <c>CARWASH_DB_CONNECTION</c>, <c>ConnectionStrings__Default</c> (padrão usado
/// pelo docker-compose), e por fim um default local.
/// </summary>
public sealed class CarWashDbContextFactory : IDesignTimeDbContextFactory<CarWashDbContext>
{
    public const string ConnectionEnvVar = "CARWASH_DB_CONNECTION";
    public const string AspNetConnectionEnvVar = "ConnectionStrings__Default";

    public CarWashDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable(ConnectionEnvVar)
            ?? Environment.GetEnvironmentVariable(AspNetConnectionEnvVar)
            ?? "Host=localhost;Port=5432;Database=carwash;Username=carwash_owner;Password=carwash123";

        var options = new DbContextOptionsBuilder<CarWashDbContext>()
            .UseNpgsql(cs, npg => npg
                .MigrationsAssembly(typeof(CarWashDbContext).Assembly.FullName)
                .MigrationsHistoryTable("__ef_migrations_history", "public"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new CarWashDbContext(options);
    }
}
