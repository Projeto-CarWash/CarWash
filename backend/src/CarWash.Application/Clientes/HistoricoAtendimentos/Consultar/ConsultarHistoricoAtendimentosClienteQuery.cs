using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Clientes.HistoricoAtendimentos.Common;

namespace CarWash.Application.Clientes.HistoricoAtendimentos.Consultar;

public sealed record ConsultarHistoricoAtendimentosClienteQuery(
    Guid ClienteId,
    DateTimeOffset? DataInicio,
    DateTimeOffset? DataFim,
    int? UltimosDias,
    string? Status,
    int Page,
    int PageSize,
    string TraceId) : IQuery<HistoricoAtendimentosResponse>;
