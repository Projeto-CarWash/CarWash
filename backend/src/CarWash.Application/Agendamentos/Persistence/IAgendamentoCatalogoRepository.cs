namespace CarWash.Application.Agendamentos.Persistence;

/// <summary>
/// Snapshot de um serviço lido do catálogo no momento do agendamento — preço e
/// duração são congelados no <see cref="Domain.Entities.AgendamentoItem"/>.
/// </summary>
public sealed record ServicoSnapshot(Guid Id, string Nome, decimal Preco, int DuracaoMin, bool Ativo);

/// <summary>
/// Porta de leitura para validar as dependências de um agendamento (filial,
/// veículo, cliente, responsável e serviços) sem acoplar a Application ao EF Core.
/// </summary>
public interface IAgendamentoCatalogoRepository
{
    /// <summary>Retorna <c>true</c> se a filial existe e está ativa (RF019/RN010).</summary>
    Task<bool> FilialAtivaAsync(Guid filialId, CancellationToken cancellationToken);

    /// <summary>Retorna <c>true</c> se a filial existe (independente de estar ativa).</summary>
    Task<bool> FilialExisteAsync(Guid filialId, CancellationToken cancellationToken);

    /// <summary>
    /// Retorna o <c>ClienteId</c> dono do veículo quando o veículo existe e está
    /// ativo; <c>null</c> caso não exista; e o id com <c>Ativo=false</c> quando
    /// existe porém inativo.
    /// </summary>
    Task<VeiculoSnapshot?> ObterVeiculoAsync(Guid veiculoId, CancellationToken cancellationToken);

    /// <summary>Retorna <c>true</c> se o cliente existe e está ativo.</summary>
    Task<bool> ClienteAtivoAsync(Guid clienteId, CancellationToken cancellationToken);

    /// <summary>Retorna <c>true</c> se o cliente existe (independente de estar ativo).</summary>
    Task<bool> ClienteExisteAsync(Guid clienteId, CancellationToken cancellationToken);

    /// <summary>
    /// Retorna <c>true</c> se o responsável existe, está ativo e pertence ao
    /// cliente informado (CA009 — responsável só agenda em nome do seu titular).
    /// </summary>
    Task<ResponsavelSnapshot?> ObterResponsavelAsync(Guid responsavelId, CancellationToken cancellationToken);

    /// <summary>
    /// Carrega os serviços pedidos. Itens ausentes do retorno indicam serviço
    /// inexistente; o flag <c>Ativo</c> indica serviço fora de catálogo.
    /// </summary>
    Task<IReadOnlyList<ServicoSnapshot>> ObterServicosAsync(
        IReadOnlyCollection<Guid> servicoIds,
        CancellationToken cancellationToken);
}

/// <summary>Projeção mínima de um veículo para validação de agendamento.</summary>
public sealed record VeiculoSnapshot(Guid Id, Guid ClienteId, bool Ativo);

/// <summary>Projeção mínima de um responsável (filiado) para validação de agendamento.</summary>
public sealed record ResponsavelSnapshot(Guid Id, Guid ClienteId, bool Ativo);
