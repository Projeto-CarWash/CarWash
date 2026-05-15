using CarWash.Application.Usuarios.Persistence;
using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarWash.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação concreta de <see cref="IUsuarioRepository"/> sobre EF Core.
/// Mantém a Application desacoplada do <see cref="CarWashDbContext"/>.
/// </summary>
public sealed class UsuarioRepository : IUsuarioRepository
{
    private readonly CarWashDbContext _db;

    public UsuarioRepository(CarWashDbContext db)
    {
        _db = db;
    }

    public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<Usuario?> ObterPorIdRastreadoAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<Usuario?> ObterPorEmailAsync(string emailNormalizado, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(emailNormalizado))
        {
            return Task.FromResult<Usuario?>(null);
        }

        return _db.Usuarios.FirstOrDefaultAsync(u => u.EmailValor == emailNormalizado, cancellationToken);
    }

    public Task<bool> ExisteComEmailAsync(string emailNormalizado, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(emailNormalizado);
        return _db.Usuarios.AsNoTracking().AnyAsync(u => u.EmailValor == emailNormalizado, cancellationToken);
    }

    public Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        _db.Usuarios.Add(usuario);
        return Task.CompletedTask;
    }

    public Task SalvarAsync(CancellationToken cancellationToken) =>
        _db.SaveChangesAsync(cancellationToken);
}
