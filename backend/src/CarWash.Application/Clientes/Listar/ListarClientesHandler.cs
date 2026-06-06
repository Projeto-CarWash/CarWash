using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Clientes.Common;
using CarWash.Application.Clientes.Persistence;

namespace CarWash.Application.Clientes.Listar;

public sealed class ListarClientesHandler : IQueryHandler<ListarClientesQuery, ListaClientesResponse>
{
    private readonly IClienteRepository _repositorio;

    public ListarClientesHandler(IClienteRepository repositorio)
    {
        _repositorio = repositorio;
    }

    /// <inheritdoc/>
    public async Task<ListaClientesResponse> HandleAsync(ListarClientesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (itens, total) = await _repositorio.ListarAsync(
            query.Busca,
            query.Ativo,
            query.Pagina,
            query.TamanhoPagina,
            cancellationToken).ConfigureAwait(false);

        // GAP-CLAMP: reflete o tamanho efetivo (clamp aplicado no repositório),
        // não o valor pedido pelo cliente. O endpoint já rejeita fora da faixa,
        // mas se algum chamador interno bypassar a validação, o JSON ainda é honesto.
        int paginaEfetiva = query.Pagina < 1 ? 1 : query.Pagina;
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

        return new ListaClientesResponse
        {
            Total = total,
            Pagina = paginaEfetiva,
            TamanhoPagina = tamanhoEfetivo,
            Itens = itens.Select(ClienteResumoResponse.FromEntity).ToList(),
        };
    }
}
