using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Usuarios.Common;
using CarWash.Application.Usuarios.Persistence;

namespace CarWash.Application.Usuarios.Listar;

public sealed class ListarUsuariosHandler : IQueryHandler<ListarUsuariosQuery, ListaUsuariosResponse>
{
    private readonly IUsuarioRepository _repo;

    public ListarUsuariosHandler(IUsuarioRepository repo)
    {
        _repo = repo;
    }

    /// <inheritdoc/>
    public async Task<ListaUsuariosResponse> HandleAsync(ListarUsuariosQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (itens, total) = await _repo.ListarAsync(
            query.Busca,
            query.Ativo,
            query.Pagina,
            query.TamanhoPagina,
            cancellationToken).ConfigureAwait(false);

        int pagina = query.Pagina < 1 ? 1 : query.Pagina;
        int tamanho = query.TamanhoPagina < 1 ? 20 : query.TamanhoPagina;

        return new ListaUsuariosResponse(
            itens.Select(UsuarioResponse.FromEntity).ToList(),
            total,
            pagina,
            tamanho);
    }
}
