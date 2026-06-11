using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Responsaveis.Common;
using CarWash.Application.Responsaveis.Persistence;

namespace CarWash.Application.Responsaveis.Listar;

public sealed class ListarResponsaveisHandler : IQueryHandler<ListarResponsaveisQuery, ListaResponsaveisResponse>
{
    private readonly IResponsavelRepository _repositorio;

    public ListarResponsaveisHandler(IResponsavelRepository repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<ListaResponsaveisResponse> HandleAsync(ListarResponsaveisQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var responsaveis = await _repositorio.ListarPorClienteTitularIdAsync(query.ClienteTitularId, cancellationToken).ConfigureAwait(false);

        return new ListaResponsaveisResponse
        {
            Itens = responsaveis.Select(ResponsavelResponse.FromEntity).ToList(),
        };
    }
}
