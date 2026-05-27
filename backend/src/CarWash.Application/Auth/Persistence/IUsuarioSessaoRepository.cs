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
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    Task<UsuarioSessao?> ObterPorHashAsync(string refreshTokenHash, CancellationToken cancellationToken);

    /// <summary>
    /// Busca sessão pelo hash SHA-256 com <c>SELECT ... FOR UPDATE</c> (lock pessimista
    /// no nível de linha do PostgreSQL). DEVE ser chamado dentro de uma transação aberta
    /// — caso contrário o lock é liberado imediatamente. Bloqueia leitores concorrentes
    /// que tentem o mesmo <c>FOR UPDATE</c> até o COMMIT/ROLLBACK desta transação,
    /// prevenindo a race do BUG-010 em <c>/refresh</c> paralelo. Quando a transação
    /// concorrente liberar o lock, a leitura aqui já enxergará <c>revogado_em</c>
    /// preenchido e cairá no caminho de reuse-detection (CA011).
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    Task<UsuarioSessao?> ObterPorHashParaAtualizacaoAsync(string refreshTokenHash, CancellationToken cancellationToken);

    /// <summary>
    /// Abre uma transação no <c>DbContext</c> subjacente e a retorna como
    /// <see cref="IUsuarioSessaoTransacao"/>. O chamador é responsável por
    /// <c>CommitAsync</c>/<c>RollbackAsync</c> e por dispor o handle (idealmente
    /// via <c>await using</c>). Quando já existir uma transação ativa, retorna um
    /// handle "no-op" para evitar BEGIN aninhado.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    Task<IUsuarioSessaoTransacao> IniciarTransacaoAsync(CancellationToken cancellationToken);

    /// <summary>Adiciona uma nova sessão ao contexto (não persiste ainda).</summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    Task AdicionarAsync(UsuarioSessao sessao, CancellationToken cancellationToken);

    /// <summary>
    /// Revoga (marca <c>revogado_em</c>) TODAS as sessões ativas (não revogadas e
    /// não expiradas) do usuário em uma única operação. Usado quando se detecta
    /// reuse de refresh token — política de "global revoke da família" do OAuth 2.1
    /// (CA011). Retorna o número de sessões afetadas. Persiste em seguida.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    Task<int> RevogarTodasAtivasDoUsuarioAsync(Guid usuarioId, DateTime quandoUtc, CancellationToken cancellationToken);

    /// <summary>Persiste alterações pendentes (insert/update).</summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    Task SalvarAsync(CancellationToken cancellationToken);
}
