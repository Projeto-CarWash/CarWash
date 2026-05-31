using CarWash.Domain.Entities;

namespace CarWash.Application.Interfaces;

public interface IAgendamentoObservacaoRepository
{
    Task<bool> AgendamentoExisteAsync(
        Guid agendamentoId,
        CancellationToken cancellationToken);

    Task<AgendamentoObservacao?> ObterPorIdEAgendamentoAsync(
        Guid observacaoId,
        Guid agendamentoId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AgendamentoObservacao>> ListarPorAgendamentoAsync(
        Guid agendamentoId,
        bool incluirInativas,
        CancellationToken cancellationToken);

    Task AdicionarAsync(
        AgendamentoObservacao observacao,
        string traceId,
        CancellationToken cancellationToken);

    Task AtualizarAsync(
        AgendamentoObservacao observacao,
        string textoAnterior,
        string traceId,
        CancellationToken cancellationToken);

    Task ExcluirAsync(
        AgendamentoObservacao observacao,
        string textoAnterior,
        string traceId,
        CancellationToken cancellationToken);
}
