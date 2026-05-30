namespace CarWash.Application.Auth.Persistence;

/// <summary>
/// Handle de transação opaco para o fluxo de rotação de refresh token. Encapsula
/// <c>IDbContextTransaction</c> da Infrastructure para que a Application não
/// dependa do EF Core diretamente. Implementa <see cref="IAsyncDisposable"/> —
/// o caller deve usar <c>await using</c>. Não-commit antes do dispose equivale a
/// rollback.
/// </summary>
public interface IUsuarioSessaoTransacao : IAsyncDisposable
{
    /// <summary>Confirma a transação (COMMIT). Idempotente em handles no-op.</summary>
    Task CommitAsync(CancellationToken cancellationToken);

    /// <summary>Desfaz a transação (ROLLBACK). Idempotente em handles no-op.</summary>
    Task RollbackAsync(CancellationToken cancellationToken);
}
