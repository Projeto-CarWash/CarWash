using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Agendamentos.Persistence;
using CarWash.Application.Common.Exceptions;

namespace CarWash.Application.Agendamentos.ObterPorId;

public sealed class ObterAgendamentoPorIdHandler : IQueryHandler<ObterAgendamentoPorIdQuery, ObterAgendamentoPorIdResponse>
{
    public const string MensagemNaoEncontrado = "Agendamento não encontrado.";

    private readonly IAgendamentoRepository _repositorio;

    public ObterAgendamentoPorIdHandler(IAgendamentoRepository repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<ObterAgendamentoPorIdResponse> HandleAsync(
        ObterAgendamentoPorIdQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var agendamento = await _repositorio.ObterPorIdAsync(query.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(MensagemNaoEncontrado);

        return new ObterAgendamentoPorIdResponse
        {
            Data = ObterAgendamentoPorIdData.FromEntity(agendamento),
        };
    }
}
