using CarWash.Application.Abstractions.Messaging;

namespace CarWash.Application.Servicos.Listar;

public sealed record ListarServicosQuery(
    bool? Ativo) : IQuery<ListaServicosResponse>;
