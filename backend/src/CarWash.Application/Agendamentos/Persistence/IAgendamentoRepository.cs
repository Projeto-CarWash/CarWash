using CarWash.Domain.Entities;

namespace CarWash.Application.Agendamentos.Persistence;

/// <summary>
/// Porta de persistência do agregado <see cref="Agendamento"/>. A implementação
/// concreta vive em <c>CarWash.Infrastructure</c> — mantém a Application
/// desacoplada do EF Core.
/// </summary>
public interface IAgendamentoRepository
{
    /// <summary>
    /// Verifica se o veículo já possui um agendamento ativo (status
    /// <c>agendado</c>) cuja janela <c>[inicio, fim)</c> se sobrepõe à informada.
    /// Pré-check da RN011/RF020 — independente de filial. A defesa final é a
    /// constraint EXCLUDE <c>ex_ag_veiculo_janela</c> no banco.
    /// </summary>
    Task<bool> ExisteConflitoVeiculoAsync(
        Guid veiculoId,
        DateTime inicio,
        DateTime fim,
        CancellationToken cancellationToken);

    /// <summary>
    /// Persiste o agendamento, seus itens e o evento <c>CRIADO</c> do histórico
    /// numa única transação. Em violação da EXCLUDE <c>ex_ag_veiculo_janela</c>
    /// (race condition), lança <see cref="Common.AgendamentoConflitanteException"/>.
    /// </summary>
    Task AdicionarAsync(
        Agendamento agendamento,
        IReadOnlyCollection<AgendamentoItem> itens,
        AgendamentoHistorico historico,
        string correlationId,
        CancellationToken cancellationToken);
}
