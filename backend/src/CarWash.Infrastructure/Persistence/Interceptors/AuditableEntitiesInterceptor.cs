using CarWash.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CarWash.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Preenche <c>CriadoEm</c> e <c>AtualizadoEm</c> em qualquer entidade que implemente
/// <see cref="IAuditable"/> antes do <c>SaveChanges</c> (DB001 §07.5).
/// </summary>
public sealed class AuditableEntitiesInterceptor : SaveChangesInterceptor
{
    /// <inheritdoc/>
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        AtualizarTimestamps(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AtualizarTimestamps(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void AtualizarTimestamps(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var agora = DateTime.UtcNow;
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is not IAuditableSetter setter)
            {
                continue;
            }

            switch (entry.State)
            {
                case EntityState.Added:
                    setter.SetCriadoEm(agora);
                    setter.SetAtualizadoEm(agora);
                    break;
                case EntityState.Modified:
                    setter.SetAtualizadoEm(agora);
                    PreservarCriadoEm(entry);
                    break;
            }
        }
    }

    private static void PreservarCriadoEm(EntityEntry entry)
    {
        var prop = entry.Property(nameof(IAuditable.CriadoEm));
        prop.IsModified = false;
    }
}
