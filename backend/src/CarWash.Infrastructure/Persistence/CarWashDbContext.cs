using CarWash.Application.Interfaces; // <-- 1. Adicionamos o using para achar a interface
using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarWash.Infrastructure.Persistence;

// <-- 2. Colocamos o ICarWashDbContext aqui do lado do DbContext
public class CarWashDbContext : DbContext, ICarWashDbContext
{
    public CarWashDbContext(DbContextOptions<CarWashDbContext> options)
        : base(options)
    {
    }

    /// <inheritdoc/>
    public DbSet<User> Users { get; set; }

    /// <inheritdoc/>
    public DbSet<Session> Sessions { get; set; }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(CarWashDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
