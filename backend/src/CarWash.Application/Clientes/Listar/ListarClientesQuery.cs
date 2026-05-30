using CarWash.Application.Abstractions.Messaging;

namespace CarWash.Application.Clientes.Listar;

/// <summary>
/// Listagem paginada de clientes com filtro por nome/documento/email/cidade.
/// O endpoint exige <c>Cache-Control: no-store</c> (PII).
/// </summary>
public sealed record ListarClientesQuery(
    string? Busca,
    bool? Ativo,
    int Pagina,
    int TamanhoPagina) : IQuery<ListaClientesResponse>;
