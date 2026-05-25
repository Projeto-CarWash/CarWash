using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Servicos.Common;

namespace CarWash.Application.Servicos.ObterPorId;

/// <summary>
/// Consulta detalhada por id do serviço.
/// </summary>
public sealed record ObterServicoPorIdQuery(Guid Id) : IQuery<ServicoResponse>;
