using CarWash.Domain.Entities;

namespace CarWash.Application.Usuarios.Persistence;

/// <summary>
/// Porta de persistência da aggregate <see cref="Usuario"/>. A implementação concreta
/// vive em <c>CarWash.Infrastructure</c>. Mantém a Application desacoplada do EF Core.
/// </summary>
public interface IUsuarioRepository
{
    /// <summary>Recupera por id (AsNoTracking) ou retorna <c>null</c>.</summary>
    Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Recupera por id com tracking habilitado — uso obrigatório quando a Use Case
    /// vai mutar o agregado antes de <see cref="SalvarAsync"/>.
    /// </summary>
    Task<Usuario?> ObterPorIdRastreadoAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Recupera por e-mail normalizado (lowercase) com tracking habilitado — usado
    /// no fluxo de login (rehash, atualização de senha).
    /// </summary>
    Task<Usuario?> ObterPorEmailAsync(string emailNormalizado, CancellationToken cancellationToken);

    /// <summary>Verifica se já existe um usuário com o e-mail (case-insensitive — coluna persistida lower).</summary>
    Task<bool> ExisteComEmailAsync(string emailNormalizado, CancellationToken cancellationToken);

    /// <summary>Adiciona o aggregate à unidade de trabalho — não persiste.</summary>
    Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken);

    /// <summary>Persiste as mudanças. Pode lançar <c>DbUpdateException</c> em violação de UK.</summary>
    Task SalvarAsync(CancellationToken cancellationToken);
}
