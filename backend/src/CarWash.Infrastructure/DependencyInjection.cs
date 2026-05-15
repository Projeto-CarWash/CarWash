using CarWash.Application.Interfaces;
using CarWash.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CarWash.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        string conn = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default não configurada");

        services.AddDbContext<CarWashDbContext>(opt =>
            opt.UseNpgsql(conn, npg => npg.MigrationsHistoryTable("__ef_migrations_history", "public"))
               .UseSnakeCaseNamingConvention());

        services.AddScoped<ICarWashDbContext, CarWashDbContext>();

        return services;
    }
}
