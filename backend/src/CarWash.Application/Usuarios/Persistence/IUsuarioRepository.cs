using CarWash.Domain.Entities;

namespace CarWash.Application.Usuarios.Persistence;

/// <summary>
/// Porta de persistência da aggregate <see cref="Usuario"/>. A implementação concreta
/// vive em <c>CarWash.Infrastructure</c>. Mantém a Application desacoplada do EF Core.
/// </summary>
public interface IUsuarioRepository
{
    /// <summary>Recupera por id (AsNoTracking) ou retorna <c>null</c>.</summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Recupera por id com tracking habilitado — uso obrigatório quando a Use Case
    /// vai mutar o agregado antes de <see cref="SalvarAsync"/>.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    Task<Usuario?> ObterPorIdRastreadoAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Recupera por e-mail normalizado (lowercase) com tracking habilitado — usado
    /// no fluxo de login (rehash, atualização de senha).
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    Task<Usuario?> ObterPorEmailAsync(string emailNormalizado, CancellationToken cancellationToken);

    /// <summary>Verifica se já existe um usuário com o e-mail (case-insensitive — coluna persistida lower).</summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    Task<bool> ExisteComEmailAsync(string emailNormalizado, CancellationToken cancellationToken);

    /// <summary>Adiciona o aggregate à unidade de trabalho — não persiste.</summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken);

    /// <summary>Persiste as mudanças. Pode lançar <c>DbUpdateException</c> em violação de UK.</summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    Task SalvarAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Listagem paginada de usuários para gestão (RF014). Filtros opcionais:
    /// busca livre por nome/email (ILIKE) e status (<c>ativo</c>). Ordenação por <c>nome ASC</c>.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    Task<(IReadOnlyList<Usuario> Itens, int Total)> ListarAsync(
        string? busca,
        bool? ativo,
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken);

    /// <summary>
    /// Conta usuários com perfil <c>ADMIN</c> e <c>ativo = true</c>. Usado pela RN de
    /// "não permitir desativar o último admin ativo" no endpoint de status (BUG-U009).
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    Task<int> ContarAdminsAtivosAsync(CancellationToken cancellationToken);
}
