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
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task AdicionarAsync(
        Agendamento agendamento,
        IReadOnlyCollection<AgendamentoItem> itens,
        AgendamentoHistorico historico,
        string correlationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Persiste o agendamento, seus itens, o histórico e o registro de
    /// idempotência (RF015) numa única transação. Trata duas violações:
    /// a EXCLUDE de conflito de veículo (RN011) → <see cref="Common.AgendamentoConflitanteException"/>;
    /// e a UNIQUE <c>uq_idempotencia_key_escopo</c> — relê o registro vencedor e
    /// devolve <see cref="ResultadoConfirmacaoIdempotente"/> com a resposta gravada
    /// (replay) ou lança <see cref="Common.IdempotenciaConflitanteException"/> se
    /// o payload diverge.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<ResultadoConfirmacaoIdempotente> AdicionarComIdempotenciaAsync(
        Agendamento agendamento,
        IReadOnlyCollection<AgendamentoItem> itens,
        AgendamentoHistorico historico,
        IdempotenciaRequisicao idempotencia,
        string correlationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Obtém o agendamento por <paramref name="id"/>, rastreado pelo change tracker
    /// do EF Core, para permitir alterações posteriores via <see cref="SalvarAsync"/>.
    /// Retorna <c>null</c> se não existir.
    /// </summary>
    Task<Agendamento?> ObterPorIdRastreadoAsync(
        Guid id,
        CancellationToken cancellationToken);

    /// <summary>
    /// Persiste alterações num agendamento já rastreado (cancelamento, etc.),
    /// incluindo o evento de histórico e o log de auditoria, numa única transação.
    /// A concorrência otimista usa <c>Versao</c> (concurrency token) — se o
    /// agendamento foi modificado por outra transação, lança
    /// <see cref="DbUpdateConcurrencyException"/>.
    /// </summary>
    Task SalvarAsync(
        Agendamento agendamento,
        AgendamentoHistorico historico,
        string correlationId,
        CancellationToken cancellationToken);
}

/// <summary>
/// Resultado da persistência idempotente de uma confirmação. Quando
/// <see cref="EhReplay"/> é <c>true</c>, <see cref="RespostaJsonOriginal"/> traz
/// o corpo da resposta gravada na primeira chamada (a transação atual foi
/// descartada); caso contrário, a confirmação foi efetivamente persistida.
/// </summary>
public sealed record ResultadoConfirmacaoIdempotente(bool EhReplay, string? RespostaJsonOriginal)
{
    /// <summary>Confirmação persistida agora (sem replay).</summary>
    /// <returns></returns>
    public static ResultadoConfirmacaoIdempotente Persistido() => new(false, null);

    /// <summary>Replay: a chave já existia com o mesmo payload — devolve a resposta original.</summary>
    /// <returns></returns>
    public static ResultadoConfirmacaoIdempotente Replay(string respostaJsonOriginal) =>
        new(true, respostaJsonOriginal);
}
