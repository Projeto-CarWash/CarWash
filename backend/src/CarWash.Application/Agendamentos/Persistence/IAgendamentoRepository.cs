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
    /// <c>agendado</c> ou <c>em_andamento</c>) cuja janela <c>[inicio, fim)</c>
    /// se sobrepõe à informada. Pré-check da RN011/RF020 — independente de filial.
    /// A defesa final é a constraint EXCLUDE <c>ex_ag_veiculo_janela</c> no banco.
    /// </summary>
    Task<bool> ExisteConflitoVeiculoAsync(
        Guid veiculoId,
        DateTime inicio,
        DateTime fim,
        CancellationToken cancellationToken);

    /// <summary>
    /// Verifica se a filial já atingiu sua capacidade de atendimentos simultâneos
    /// (RF008/RN009): retorna <c>true</c> quando a quantidade de agendamentos ativos
    /// (status <c>agendado</c>) cuja janela <c>[inicio, fim)</c> se sobrepõe à
    /// informada é maior ou igual a <c>celulas_ativas</c> da filial. Permite
    /// múltiplos agendamentos no mesmo horário até o teto de células. Pré-check
    /// (sem garantia anti-corrida no banco — ver nota nos handlers). Retorna
    /// <c>false</c> para filial inexistente (a existência é validada antes pela
    /// <c>CalculadoraResumoAgendamento</c>).
    /// </summary>
    Task<bool> CapacidadeAtingidaAsync(
        Guid filialId,
        DateTime inicio,
        DateTime fim,
        CancellationToken cancellationToken);

    /// <summary>
    /// Conta agendamentos na janela <c>[inicio, fim)</c> por status de ocupação
    /// (<c>agendado</c> e <c>em_andamento</c>) — RF008: capacidade considera ambos
    /// os status para cálculo correto de vagas.
    /// </summary>
    Task<int> ContarOcupacaoAsync(
        Guid filialId,
        DateTime inicio,
        DateTime fim,
        CancellationToken cancellationToken);

    /// <summary>
    /// Persiste o agendamento, seus itens e o evento <c>CRIADO</c> do histórico
    /// numa única transação. Em violação da EXCLUDE <c>ex_ag_veiculo_janela</c>
    /// (race condition), lança <see cref="Common.AgendamentoConflitanteException"/>.
    /// RF024/CA009: revalida o vínculo responsável→cliente sob <c>SELECT FOR UPDATE</c>
    /// dentro da transação — se o vínculo foi alterado concorrentemente, lança
    /// <see cref="Common.ConflictException"/> com slug <c>responsavel-nao-vinculado</c>.
    /// </summary>
    Task AdicionarAsync(
        Agendamento agendamento,
        IReadOnlyCollection<AgendamentoItem> itens,
        AgendamentoHistorico historico,
        string correlationId,
        Guid responsavelId,
        Guid clienteId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Persiste o agendamento, seus itens, o histórico e o registro de
    /// idempotência (RF015) numa única transação. Trata duas violações:
    /// a EXCLUDE de conflito de veículo (RN011) → <see cref="Common.AgendamentoConflitanteException"/>;
    /// e a UNIQUE <c>uq_idempotencia_key_escopo</c> — relê o registro vencedor e
    /// devolve <see cref="ResultadoConfirmacaoIdempotente"/> com a resposta gravada
    /// (replay) ou lança <see cref="Common.IdempotenciaConflitanteException"/> se
    /// o payload diverge. RF024/CA009: revalida o vínculo responsável→cliente sob
    /// <c>SELECT FOR UPDATE</c> dentro da transação.
    /// </summary>
    Task<ResultadoConfirmacaoIdempotente> AdicionarComIdempotenciaAsync(
        Agendamento agendamento,
        IReadOnlyCollection<AgendamentoItem> itens,
        AgendamentoHistorico historico,
        IdempotenciaRequisicao idempotencia,
        string correlationId,
        Guid responsavelId,
        Guid clienteId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Obtém o agendamento por <paramref name="id"/> para leitura (RF008 GET endpoint).
    /// Retorna <c>null</c> se não existir.
    /// </summary>
    Task<Agendamento?> ObterPorIdAsync(
        Guid id,
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
    /// Obtém o agendamento com seus itens por <paramref name="id"/>, sem rastreamento
    /// (AsNoTracking), para consultas de leitura (RF010). Retorna <c>null</c> se não existir.
    /// </summary>
    Task<(Agendamento Agendamento, IReadOnlyCollection<AgendamentoItem> Itens)?> ObterPorIdComItensAsync(
        Guid id,
        CancellationToken cancellationToken);

    /// <summary>
    /// Persiste alterações num agendamento já rastreado (cancelamento, edição, etc.),
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

    /// <summary>
    /// Persiste alterações num agendamento já rastreado com evento de auditoria
    /// customizado (RF010 edição). Inclui o evento de histórico e o log de
    /// auditoria, numa única transação.
    /// </summary>
    Task SalvarAsync(
        Agendamento agendamento,
        AgendamentoHistorico historico,
        string correlationId,
        string auditEvento,
        Guid? auditUsuarioId,
        string auditDados,
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
    public static ResultadoConfirmacaoIdempotente Persistido() => new(false, null);

    /// <summary>Replay: a chave já existia com o mesmo payload — devolve a resposta original.</summary>
    public static ResultadoConfirmacaoIdempotente Replay(string respostaJsonOriginal) =>
        new(true, respostaJsonOriginal);
}
