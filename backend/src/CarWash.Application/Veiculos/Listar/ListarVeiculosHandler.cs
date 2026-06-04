using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Clientes.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Veiculos.Common;
using CarWash.Application.Veiculos.Persistence;

namespace CarWash.Application.Veiculos.Listar;

public sealed class ListarVeiculosHandler : IQueryHandler<ListarVeiculosQuery, ListaVeiculosResponse>
{
    private readonly IClienteRepository _clientes;
    private readonly IVeiculoRepository _veiculos;

    public ListarVeiculosHandler(IClienteRepository clientes, IVeiculoRepository veiculos)
    {
        _clientes = clientes;
        _veiculos = veiculos;
    }

    public async Task<ListaVeiculosResponse> HandleAsync(ListarVeiculosQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var clienteExiste = await _clientes.ObterPorIdAsync(query.ClienteId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Cliente não encontrado.");

        var (itens, total) = await _veiculos.ListarPorClienteIdAsync(
            query.ClienteId,
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

        return new ListaVeiculosResponse
        {
            Total = total,
            Pagina = paginaEfetiva,
            TamanhoPagina = tamanhoEfetivo,
            Itens = itens.Select(VeiculoResumoResponse.FromEntity).ToList(),
        };
    }
}
