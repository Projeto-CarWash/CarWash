namespace CarWash.Application.Agendamentos.Cancelar;

/// <summary>
/// DTO de entrada do <c>PATCH /api/v1/agendamentos/{id}/cancelar</c> (RF010).
/// O <c>motivoCancelamento</c> é obrigatório (trim, 5–500 chars).
/// <c>origem</c> indica a fonte do cancelamento (ex.: "USUARIO_INTERNO").
/// </summary>
public sealed class CancelarAgendamentoRequest
{
	public string? MotivoCancelamento { get; set; }

	public string? Origem { get; set; }
}
