using CarWash.Domain.Entities;

namespace CarWash.Application.Agendamentos.Persistence;

public interface IAgendamentoRepository
{
    Task<int> ContarOcupacaoAsync(
        Guid filialId, DateTime inicio, DateTime fim, CancellationToken cancellationToken);

    Task<bool> ExisteConflitoVeiculoAsync(
        Guid veiculoId, DateTime inicio, DateTime fim, CancellationToken cancellationToken);

    Task<Filial?> ObterFilialPorIdAsync(Guid filialId, CancellationToken cancellationToken);

    Task<Cliente?> ObterClientePorIdAsync(Guid clienteId, CancellationToken cancellationToken);

    Task<Veiculo?> ObterVeiculoPorIdAsync(Guid veiculoId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Servico>> ObterServicosPorIdsAsync(
        IReadOnlyList<Guid> servicoIds, CancellationToken cancellationToken);

    Task CriarAsync(
        Agendamento agendamento,
        List<AgendamentoItem> itens,
        AgendamentoHistorico historico,
        string traceId,
        Guid? usuarioId,
        CancellationToken cancellationToken);
}
