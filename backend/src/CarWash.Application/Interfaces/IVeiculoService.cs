using CarWash.Application.DTOs;

namespace CarWash.Application.Interfaces;

/// <summary>
/// Contrato para operacoes de veiculos.
/// </summary>
public interface IVeiculoService
{
    /// <summary>
    /// Cadastra um veiculo vinculado a um cliente existente.
    /// </summary>
    /// <param name="clienteId">Identificador do cliente.</param>
    /// <param name="request">Dados do veiculo.</param>
    /// <param name="traceId">Identificador de rastreio da requisicao.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Identificador do veiculo criado.</returns>
    Task<Guid> CriarVeiculoAsync(
        Guid clienteId,
        CriarVeiculoRequest request,
        string traceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza os dados de um veiculo existente.
    /// </summary>
    /// <param name="veiculoId">Identificador do veiculo.</param>
    /// <param name="request">Dados do veiculo.</param>
    /// <param name="traceId">Identificador de rastreio da requisicao.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Identificador do veiculo atualizado.</returns>
    Task<Guid> AtualizarVeiculoAsync(
        Guid veiculoId,
        CriarVeiculoRequest request,
        string traceId,
        CancellationToken cancellationToken = default);
}
