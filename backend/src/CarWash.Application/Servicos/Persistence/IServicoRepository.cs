using CarWash.Domain.Entities;

namespace CarWash.Application.Servicos.Persistence;

/// <summary>
/// Porta de persistência do agregado <see cref="Servico"/>. A implementação
/// concreta vive em <c>CarWash.Infrastructure</c>. Mantém a Application
/// desacoplada do EF Core.
/// </summary>
public interface IServicoRepository
{
    /// <summary>
    /// Verifica se já existe serviço com o nome informado. Quando
    /// <paramref name="ignoreServicoId"/> é informado, ignora o próprio serviço
    /// (usado no PATCH para permitir manter o mesmo nome).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<bool> ExisteNomeAsync(string nome, Guid? ignoreServicoId, CancellationToken cancellationToken);

    Task<Servico?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    Task AdicionarAsync(Servico servico, string correlationId, Guid? usuarioId, CancellationToken cancellationToken);

    /// <summary>Persiste alterações pendentes (Update).</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SalvarAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Registra evento de auditoria (edição, ativação/desativação) com traceId.
    /// Persiste imediatamente no banco.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task RegistrarAuditoriaAsync(
        string evento,
        Guid entidadeId,
        string correlationId,
        Guid? usuarioId,
        string? dados,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lista serviços paginados com filtro opcional por nome / ativo.
    /// Ordenação por <c>nome ASC</c>.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<(IReadOnlyList<Servico> Itens, int Total)> ListarAsync(
        string? busca,
        bool? ativo,
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken);
}
