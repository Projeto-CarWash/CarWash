using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Servicos.Common;
using CarWash.Application.Servicos.Persistence;

namespace CarWash.Application.Servicos.ObterPorId;

public sealed class ObterServicoPorIdHandler : IQueryHandler<ObterServicoPorIdQuery, ServicoResponse>
{
    public const string MensagemNaoEncontrado = "Serviço não encontrado.";

    private readonly IServicoRepository _repositorio;

    public ObterServicoPorIdHandler(IServicoRepository repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<ServicoResponse> HandleAsync(ObterServicoPorIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var servico = await _repositorio.ObterPorIdAsync(query.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(MensagemNaoEncontrado);

        return ServicoResponse.FromEntity(servico);
    }
}
