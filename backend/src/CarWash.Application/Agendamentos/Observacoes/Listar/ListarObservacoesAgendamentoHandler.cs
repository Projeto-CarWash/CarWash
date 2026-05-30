using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Agendamentos.Observacoes.Common;
using CarWash.Application.Interfaces;

namespace CarWash.Application.Agendamentos.Observacoes.Listar;

public sealed class ListarObservacoesAgendamentoHandler
    : IQueryHandler<ListarObservacoesAgendamentoQuery, ListarObservacoesAgendamentoResponse>
{
    private readonly IAgendamentoObservacaoRepository repository;

    public ListarObservacoesAgendamentoHandler(IAgendamentoObservacaoRepository repository)
    {
        this.repository = repository;
    }

    /// <inheritdoc/>
    public async Task<ListarObservacoesAgendamentoResponse> HandleAsync(
        ListarObservacoesAgendamentoQuery query,
        CancellationToken cancellationToken)
    {
        bool agendamentoExiste = await repository.AgendamentoExisteAsync(
            query.AgendamentoId,
            cancellationToken);

        if (!agendamentoExiste)
        {
            throw new AgendamentoNaoEncontradoException();
        }

        var observacoes = await repository.ListarPorAgendamentoAsync(
            query.AgendamentoId,
            query.IncluirInativas,
            cancellationToken);

        return new ListarObservacoesAgendamentoResponse
        {
            Message = "Observações logísticas consultadas com sucesso.",
            Data = observacoes
                .Select(AgendamentoObservacaoMapper.ToResponse)
                .ToList(),
            TraceId = query.TraceId,
        };
    }
}
