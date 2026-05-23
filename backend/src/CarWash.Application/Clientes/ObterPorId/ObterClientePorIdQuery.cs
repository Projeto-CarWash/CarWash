using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Clientes.Common;

namespace CarWash.Application.Clientes.ObterPorId;

/// <summary>
/// Consulta detalhada por id (PII completo — endpoint exige <c>Cache-Control: no-store</c>).
/// </summary>
public sealed record ObterClientePorIdQuery(Guid Id) : IQuery<ClienteResponse>;
