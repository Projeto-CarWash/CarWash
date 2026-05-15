using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CarWash.Infrastructure.Persistence;

/// <summary>
/// Design-time factory para <c>dotnet ef</c>. Lê a conexão da env
/// <c>CARWASH_DB_CONNECTION</c> ou cai para um default local de docker-compose.
/// </summary>
public sealed class CarWashDbContextFactory : IDesignTimeDbContextFactory<CarWashDbContext>
{
    public const string ConnectionEnvVar = "CARWASH_DB_CONNECTION";

    public CarWashDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable(ConnectionEnvVar)
            ?? "Host=localhost;Port=5432;Database=carwash;Username=carwash_owner;Password=carwash";

        var options = new DbContextOptionsBuilder<CarWashDbContext>()
            .UseNpgsql(cs, npg => npg
                .MigrationsAssembly(typeof(CarWashDbContext).Assembly.FullName)
                .MigrationsHistoryTable("__ef_migrations_history", "public"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new CarWashDbContext(options);
    }
}
