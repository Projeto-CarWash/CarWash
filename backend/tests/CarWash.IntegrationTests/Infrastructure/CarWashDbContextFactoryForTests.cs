using CarWash.Infrastructure.Persistence;
using CarWash.Infrastructure.Persistence.Interceptors;
using CarWash.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CarWash.IntegrationTests.Infrastructure;

/// <summary>
/// Helper que devolve um <see cref="CarWashDbContext"/> conectado ao container
/// da fixture — sem subir todo o <c>WebApplicationFactory</c>. Útil para os testes
/// de constraints/violations focados no schema.
/// </summary>
public static class CarWashDbContextFactoryForTests
{
    public static CarWashDbContext Create(PostgresFixture fixture, params IInterceptor[] interceptors)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        var optionsBuilder = new DbContextOptionsBuilder<CarWashDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(new AuditableEntitiesInterceptor());

        if (interceptors.Length > 0)
        {
            optionsBuilder.AddInterceptors(interceptors);
        }

        return new CarWashDbContext(optionsBuilder.Options);
    }
}
