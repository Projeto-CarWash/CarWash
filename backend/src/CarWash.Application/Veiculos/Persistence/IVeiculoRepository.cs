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
    /// Persiste o veículo. Em violação concorrente da <c>uk_veiculos_placa</c>,
    /// converte para <c>PlacaJaCadastradaException</c> (defesa em profundidade).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task AdicionarAsync(Veiculo veiculo, CancellationToken cancellationToken);
}
