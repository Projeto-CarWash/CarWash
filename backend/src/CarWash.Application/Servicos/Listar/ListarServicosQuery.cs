using CarWash.Application.Abstractions.Messaging;

namespace CarWash.Application.Servicos.Listar;

/// <summary>
/// Listagem paginada de serviços com filtro por nome / ativo.
/// </summary>
public sealed record ListarServicosQuery(
    string? Busca,
    bool? Ativo,
    int Pagina,
    int TamanhoPagina) : IQuery<ListaServicosResponse>;
