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

    public async Task<(IReadOnlyList<Usuario> Itens, int Total)> ListarAsync(
        string? busca,
        bool? ativo,
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken)
    {
        if (pagina < 1)
        {
            pagina = 1;
        }

        if (tamanhoPagina < 1)
        {
            tamanhoPagina = 20;
        }

        if (tamanhoPagina > 100)
        {
            tamanhoPagina = 100;
        }

        var query = _db.Usuarios.AsNoTracking();

        if (ativo.HasValue)
        {
            query = query.Where(u => u.Ativo == ativo.Value);
        }

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var like = $"%{busca.Trim()}%";
            query = query.Where(u =>
                EF.Functions.ILike(u.Nome, like) || EF.Functions.ILike(u.EmailValor, like));
        }

        var total = await query.CountAsync(cancellationToken);

        var itens = await query
            .OrderBy(u => u.Nome)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken);

        return (itens, total);
    }

    public Task<int> ContarAdminsAtivosAsync(CancellationToken cancellationToken) =>
        _db.Usuarios
            .AsNoTracking()
            .CountAsync(u => u.Ativo && u.PerfilRaw == "ADMIN", cancellationToken);
}
