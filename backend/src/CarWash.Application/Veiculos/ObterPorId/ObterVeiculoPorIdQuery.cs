using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Veiculos.Common;

namespace CarWash.Application.Veiculos.ObterPorId;

/// <summary>
/// Consulta detalhada de veículo por id.
/// </summary>
public sealed record ObterVeiculoPorIdQuery(Guid ClienteId, Guid VeiculoId) : IQuery<VeiculoResponse>;
