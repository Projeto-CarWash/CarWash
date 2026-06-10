using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Responsaveis.Persistence;

namespace CarWash.Application.Responsaveis.Listar;

/// <summary>
/// Use case de leitura (RF023/RF024): retorna os responsáveis vinculados ao
/// cliente titular, ordenados por nome. Retorna lista vazia quando não há
/// responsáveis (ou o cliente não existe) — adequado para popular o dropdown.
/// </summary>
public sealed class ListarResponsaveisPorClienteHandler
    : IQueryHandler<ListarResponsaveisPorClienteQuery, IReadOnlyList<ResponsavelListaItem>>
{
    private readonly IResponsavelRepository _responsaveis;

    public ListarResponsaveisPorClienteHandler(IResponsavelRepository responsaveis)
    {
        _responsaveis = responsaveis;
    }

    public async Task<IReadOnlyList<ResponsavelListaItem>> HandleAsync(
        ListarResponsaveisPorClienteQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var responsaveis = await _responsaveis
            .ListarPorClienteTitularIdAsync(query.ClienteTitularId, cancellationToken)
            .ConfigureAwait(false);

        return responsaveis
            .Select(r => new ResponsavelListaItem
            {
                Id = r.Id,
                Nome = r.Nome,
                Documento = r.Documento,
                Telefone = r.Telefone,
                Email = r.Email,
                GrauVinculo = r.GrauVinculo,
                Ativo = r.Ativo,
                CriadoEm = r.CriadoEm,
            })
            .ToList();
    }
}
