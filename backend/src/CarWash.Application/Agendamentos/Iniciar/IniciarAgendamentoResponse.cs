namespace CarWash.Application.Agendamentos.Iniciar;

/// <summary>
/// Resposta do início de atendimento. Contrato HTTP 200:
/// <c>{ message, data: { id, status, atualizadoEm }, traceId }</c> — mesmo
/// envelope da edição (RF010).
/// </summary>
public sealed class IniciarAgendamentoResponse
{
    public string Message { get; init; } = string.Empty;

    public IniciarAgendamentoData Data { get; init; } = new();

    public string TraceId { get; init; } = string.Empty;
}

public sealed class IniciarAgendamentoData
{
    public Guid Id { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTime AtualizadoEm { get; init; }
}
