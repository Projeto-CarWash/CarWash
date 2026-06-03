using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Filiais.Common;
using CarWash.Application.Filiais.Persistence;

namespace CarWash.Application.Filiais.ObterFilialPorId;

/// <summary>
/// Handler de leitura por id (AsNoTracking). 404 quando a filial não existe —
/// mensagem exata "Filial não encontrada." conforme o card do RF018.
/// </summary>
public sealed class ObterFilialPorIdHandler : IQueryHandler<ObterFilialPorIdQuery, FilialResponse>
{
    public const string MensagemNaoEncontrado = "Filial não encontrada.";

    private readonly IFilialRepository _repo;

    public ObterFilialPorIdHandler(IFilialRepository repo)
    {
        _repo = repo;
    }

    public async Task<FilialResponse> HandleAsync(ObterFilialPorIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var filial = await _repo.ObterPorIdAsync(query.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(MensagemNaoEncontrado);

        return FilialResponse.FromEntity(filial);
    }
}
