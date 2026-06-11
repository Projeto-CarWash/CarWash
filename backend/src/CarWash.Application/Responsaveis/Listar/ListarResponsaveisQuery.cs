using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Responsaveis.Common;

namespace CarWash.Application.Responsaveis.Listar;

public sealed record ListarResponsaveisQuery(Guid ClienteTitularId) : IQuery<ListaResponsaveisResponse>;
