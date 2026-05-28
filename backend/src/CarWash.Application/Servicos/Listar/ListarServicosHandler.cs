using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Servicos.Common;
using CarWash.Application.Servicos.Persistence;

namespace CarWash.Application.Servicos.Listar;

public sealed class ListarServicosHandler : IQueryHandler<ListarServicosQuery, ListaServicosResponse>
{
    private readonly IServicoRepository _repositorio;

    public ListarServicosHandler(IServicoRepository repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<ListaServicosResponse> HandleAsync(ListarServicosQuery query, CancellationToken cancellationToken)
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

        return new ListaServicosResponse
        {
            Total = total,
            Pagina = paginaEfetiva,
            TamanhoPagina = tamanhoEfetivo,
            Itens = itens.Select(ServicoResumoResponse.FromEntity).ToList(),
        };
    }
}
