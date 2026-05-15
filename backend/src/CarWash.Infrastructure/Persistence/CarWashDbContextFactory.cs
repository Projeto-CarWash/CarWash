using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CarWash.Infrastructure.Persistence;

public sealed class CarWashDbContextFactory : IDesignTimeDbContextFactory<CarWashDbContext>
{
    public CarWashDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Port=5432;Database=carwash;Username=carwash_owner;Password=carwash_password";

        DbContextOptions<CarWashDbContext> options = new DbContextOptionsBuilder<CarWashDbContext>()
            .UseNpgsql(connectionString, npg => npg.MigrationsHistoryTable("__ef_migrations_history", "public"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new CarWashDbContext(options);
    }
}
