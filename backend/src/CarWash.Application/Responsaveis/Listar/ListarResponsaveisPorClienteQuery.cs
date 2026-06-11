using CarWash.Application.Abstractions.Messaging;

namespace CarWash.Application.Responsaveis.Listar;

/// <summary>
/// Query de leitura (RF023/RF024) — lista os responsáveis vinculados a um cliente
/// titular. Alimenta o dropdown de seleção de responsável no agendamento.
/// </summary>
public sealed record ListarResponsaveisPorClienteQuery(Guid ClienteTitularId, string TraceId)
    : IQuery<IReadOnlyList<ResponsavelListaItem>>;
