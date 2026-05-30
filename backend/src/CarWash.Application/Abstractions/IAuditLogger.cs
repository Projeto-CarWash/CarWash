namespace CarWash.Application.Abstractions;

/// <summary>
/// Permite gravar eventos de auditoria que não decorrem de uma mudança de entidade
/// (ex.: <c>UsuarioLoginFalha</c>, <c>UsuarioLogado</c> sem update de domínio).
/// Os eventos derivados de mudança de entidade já são capturados pelo interceptor.
/// </summary>
public interface IAuditLogger
{
    Task LogAsync(
        string evento,
        string entidade,
        Guid? entidadeId = null,
        object? dados = null,
        CancellationToken cancellationToken = default);
}
