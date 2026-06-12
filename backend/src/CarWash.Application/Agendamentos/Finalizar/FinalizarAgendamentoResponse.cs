namespace CarWash.Application.Agendamentos.Finalizar;

/// <summary>
/// Resposta da finalização de atendimento (RF010). Contrato HTTP 200:
/// <c>{ message, data: { id, status, atualizadoEm }, traceId }</c> — mesmo
/// envelope da edição.
/// </summary>
public sealed class FinalizarAgendamentoResponse
{
    public string Message { get; init; } = string.Empty;

    public FinalizarAgendamentoData Data { get; init; } = new();

    public string TraceId { get; init; } = string.Empty;
}

public sealed class FinalizarAgendamentoData
{
    public Guid Id { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTime AtualizadoEm { get; init; }
}
