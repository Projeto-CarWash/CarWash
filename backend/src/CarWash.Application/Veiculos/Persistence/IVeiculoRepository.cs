using CarWash.Domain.Entities;

namespace CarWash.Application.Veiculos.Persistence;

/// <summary>
/// Porta de persistência do agregado <see cref="Veiculo"/>. A implementação
/// concreta vive em <c>CarWash.Infrastructure</c>. Mantém a Application
/// desacoplada do EF Core.
/// </summary>
public interface IVeiculoRepository
{
    /// <summary>
    /// Verifica se a placa (normalizada) já está cadastrada no sistema.
    /// Pré-check do RN011 — placa única global.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<bool> ExistePlacaAsync(string placaNormalizada, CancellationToken cancellationToken);

    /// <summary>
    /// Verifica se a placa (normalizada) já está cadastrada, ignorando o próprio
    /// veículo (permite manter a mesma placa em PUT/PATCH).
    /// </summary>
    Task<bool> ExistePlacaExcetoAsync(string placaNormalizada, Guid ignoreVeiculoId, CancellationToken cancellationToken);

    /// <summary>
    /// Verifica se qualquer uma das placas (normalizadas) já está cadastrada no sistema.
    /// Retorna a lista de placas que já existem no banco.
    /// </summary>
    Task<IReadOnlyCollection<string>> PlacasExistentesAsync(IEnumerable<string> placasNormalizadas, CancellationToken cancellationToken);

    Task<Veiculo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Lista veículos de um cliente paginados com filtro opcional por placa/modelo e ativo.
    /// Ordenação por <c>placa ASC</c>.
    /// </summary>
    Task<(IReadOnlyList<Veiculo> Itens, int Total)> ListarPorClienteIdAsync(
        Guid clienteId,
        string? busca,
        bool? ativo,
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken);

    /// <summary>
    /// Persiste o veículo. Em violação concorrente da <c>uk_veiculos_placa</c>,
    /// converte para <c>PlacaJaCadastradaException</c> (defesa em profundidade).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task AdicionarAsync(Veiculo veiculo, CancellationToken cancellationToken);

    /// <summary>
    /// Persiste múltiplos veículos dentro de uma transação única.
    /// Se qualquer item falhar (inválido ou duplicado), realiza rollback integral.
    /// Em violação concorrente da <c>uk_veiculos_placa</c>, converte para
    /// <c>PlacaJaCadastradaException</c>.
    /// </summary>
    Task AdicionarRangeAsync(IEnumerable<Veiculo> veiculos, CancellationToken cancellationToken);

    /// <summary>Persiste alterações pendentes (Update).</summary>
    Task SalvarAsync(CancellationToken cancellationToken);
}
