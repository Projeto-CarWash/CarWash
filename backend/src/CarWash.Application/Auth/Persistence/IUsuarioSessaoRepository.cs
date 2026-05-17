using CarWash.Domain.Entities;

namespace CarWash.Application.Auth.Persistence;

/// <summary>
/// Repositório de <see cref="UsuarioSessao"/> — usado pelo
/// <c>RefreshTokenService</c> para append + lookup por hash + revogação.
/// </summary>
public interface IUsuarioSessaoRepository
{
    /// <summary>
    /// Busca sessão pelo hash SHA-256 do refresh token. Retorna <c>null</c>
    /// quando não encontrada (não distingue "inexistente" de "expirada/revogada"
    /// para o chamador; a verificação de validade é feita por <c>EstaAtiva</c>).
    /// </summary>
    Task<UsuarioSessao?> ObterPorHashAsync(string refreshTokenHash, CancellationToken cancellationToken);

    /// <summary>Adiciona uma nova sessão ao contexto (não persiste ainda).</summary>
    Task AdicionarAsync(UsuarioSessao sessao, CancellationToken cancellationToken);

    /// <summary>Persiste alterações pendentes (insert/update).</summary>
    Task SalvarAsync(CancellationToken cancellationToken);
}
