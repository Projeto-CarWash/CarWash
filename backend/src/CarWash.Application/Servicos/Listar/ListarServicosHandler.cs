using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Interfaces;
using CarWash.Application.Servicos.Common;
using Microsoft.EntityFrameworkCore;

namespace CarWash.Application.Servicos.Listar;

public sealed class ListarServicosHandler : IQueryHandler<ListarServicosQuery, ListaServicosResponse>
{
    private readonly ICarWashDbContext _context;

    public ListarServicosHandler(ICarWashDbContext context)
    {
        _context = context;
    }

    public async Task<ListaServicosResponse> HandleAsync(ListarServicosQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var dbQuery = _context.Servicos.AsNoTracking();

        if (query.Ativo.HasValue)
        {
            dbQuery = dbQuery.Where(s => s.Ativo == query.Ativo.Value);
        }

        var itens = await dbQuery
            .OrderBy(s => s.Nome)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new ListaServicosResponse
        {
            Itens = itens.Select(ServicoResponse.FromEntity).ToList(),
            Total = itens.Count
        };
    }
}
