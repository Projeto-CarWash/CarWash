using Microsoft.EntityFrameworkCore;

namespace CarWash.Infrastructure.Persistence;

/// <summary>
/// Fábrica em tempo de execução para <see cref="CarWashDbContext"/> com
/// <see cref="DbContextOptions{TContext}"/> próprias (sem interceptors de
/// auditoria). Usada por consumidores que precisam de um DbContext
/// independente do escopo da request — por exemplo, o <c>AuditLogger</c>,
/// que precisa persistir mesmo se a transação principal sofrer rollback,
/// e não deve auditar a si próprio.
/// </summary>
/// <remarks>
/// Registrada manualmente como Singleton em <c>DependencyInjection</c> para
/// evitar conflito de lifetime entre <c>AddDbContext</c> (que registra
/// <c>DbContextOptions&lt;CarWashDbContext&gt;</c> como Scoped) e o helper
/// <c>AddDbContextFactory</c> (que tenta registrá-las como Singleton).
/// </remarks>
internal sealed class CarWashRuntimeDbContextFactory : IDbContextFactory<CarWashDbContext>
{
    private readonly DbContextOptions<CarWashDbContext> _options;

    public CarWashRuntimeDbContextFactory(DbContextOptions<CarWashDbContext> options)
    {
        _options = options;
    }

    /// <inheritdoc/>
    public CarWashDbContext CreateDbContext() => new(_options);
}
