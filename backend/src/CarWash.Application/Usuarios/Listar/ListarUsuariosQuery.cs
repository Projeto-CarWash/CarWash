using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Usuarios.Common;

namespace CarWash.Application.Usuarios.Listar;

public sealed record ListarUsuariosQuery(
    string? Busca,
    bool? Ativo,
    int Pagina,
    int TamanhoPagina) : IQuery<ListaUsuariosResponse>;

public sealed record ListaUsuariosResponse(
    IReadOnlyList<UsuarioResponse> Itens,
    int Total,
    int Pagina,
    int TamanhoPagina);
