namespace CarWash.Application.Agendamentos.Cancelar;

/// <summary>
/// Resposta do cancelamento de agendamento (RF010). Contrato HTTP 200:
/// <c>{ message, data: { id, status, canceladoEm, canceladoPor, motivoCancelamento }, traceId }</c>.
/// </summary>
public sealed class CancelarAgendamentoResponse
{
	public string Message { get; init; } = string.Empty;

	public CancelarAgendamentoData Data { get; init; } = new();

	public string TraceId { get; init; } = string.Empty;
}

public sealed class CancelarAgendamentoData
{
	public Guid Id { get; init; }

	public string Status { get; init; } = string.Empty;

	public DateTime? CanceladoEm { get; init; }

	public Guid? CanceladoPor { get; init; }

	public string? MotivoCancelamento { get; init; }
}
