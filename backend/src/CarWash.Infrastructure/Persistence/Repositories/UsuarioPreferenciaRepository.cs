using CarWash.Application.Usuarios.Preferencias.Persistence;
using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarWash.Infrastructure.Persistence.Repositories;

public sealed class UsuarioPreferenciaRepository : IUsuarioPreferenciaRepository
{
    private readonly CarWashDbContext _db;

    public UsuarioPreferenciaRepository(CarWashDbContext db)
    {
        _db = db;
    }

    public Task<UsuarioPreferencia?> ObterPorUsuarioIdAsync(
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        return _db.UsuarioPreferencias
            .FirstOrDefaultAsync(x => x.UsuarioId == usuarioId, cancellationToken);
    }

    public Task<bool> UsuarioExisteAsync(
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        return _db.Usuarios
            .AsNoTracking()
            .AnyAsync(x => x.Id == usuarioId, cancellationToken);
    }

    public async Task AdicionarAsync(
        UsuarioPreferencia preferencia,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(preferencia);

        await _db.UsuarioPreferencias
            .AddAsync(preferencia, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task SalvarAsync(CancellationToken cancellationToken)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
