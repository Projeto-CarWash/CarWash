using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Filiais.Common;
using CarWash.Application.Filiais.Persistence;

namespace CarWash.Application.Filiais.Listar;

public sealed class ListarFiliaisHandler : IQueryHandler<ListarFiliaisQuery, ListaFiliaisResponse>
{
    private readonly IFilialRepository _repositorio;

    public ListarFiliaisHandler(IFilialRepository repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<ListaFiliaisResponse> HandleAsync(ListarFiliaisQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (itens, total) = await _repositorio.ListarAsync(
            query.Busca,
            query.Ativo,
            query.Pagina,
            query.TamanhoPagina,
            cancellationToken).ConfigureAwait(false);

        var paginaEfetiva = query.Pagina < 1 ? 1 : query.Pagina;
        int tamanhoEfetivo;
        if (query.TamanhoPagina < 1)
        {
            tamanhoEfetivo = 20;
        }
        else if (query.TamanhoPagina > 100)
        {
            tamanhoEfetivo = 100;
        }
        else
        {
            tamanhoEfetivo = query.TamanhoPagina;
        }

        return new ListaFiliaisResponse
        {
            Total = total,
            Pagina = paginaEfetiva,
            TamanhoPagina = tamanhoEfetivo,
            Itens = itens.Select(FilialResumoResponse.FromEntity).ToList(),
        };
    }
}
