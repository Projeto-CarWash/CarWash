using CarWash.Application.Abstractions.Messaging;

namespace CarWash.Application.Veiculos.Listar;

/// <summary>
/// Listagem paginada de veículos de um cliente com filtro por placa/modelo/fabricante e ativo.
/// </summary>
public sealed record ListarVeiculosQuery(
    Guid ClienteId,
    string? Busca,
    bool? Ativo,
    int Pagina,
    int TamanhoPagina) : IQuery<ListaVeiculosResponse>;
