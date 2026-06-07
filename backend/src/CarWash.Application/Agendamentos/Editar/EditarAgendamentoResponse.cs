namespace CarWash.Application.Agendamentos.Editar;

/// <summary>
/// Resposta da edição de agendamento (RF010). Contrato HTTP 200:
/// <c>{ message, data: { id, status, atualizadoEm }, traceId }</c>.
/// </summary>
public sealed class EditarAgendamentoResponse
{
    public string Message { get; init; } = string.Empty;

    public EditarAgendamentoData Data { get; init; } = new();

    public string TraceId { get; init; } = string.Empty;
}

public sealed class EditarAgendamentoData
{
    public Guid Id { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTime AtualizadoEm { get; init; }
}
