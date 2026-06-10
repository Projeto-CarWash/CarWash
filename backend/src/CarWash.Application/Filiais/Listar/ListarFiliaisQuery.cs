using CarWash.Application.Abstractions.Messaging;

namespace CarWash.Application.Filiais.Listar;

/// <summary>
/// Listagem paginada de filiais (RF017 §4.2 do ADR-0007). Frontend chama
/// <c>?ativo=true</c>; <c>busca</c> casa em nome / codigo / cidade
/// (case-insensitive).
/// </summary>
public sealed record ListarFiliaisQuery(
    string? Busca,
    bool? Ativo,
    int Pagina,
    int TamanhoPagina) : IQuery<ListaFiliaisResponse>;
