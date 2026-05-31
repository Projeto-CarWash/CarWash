using CarWash.Application.Common.Exceptions;

namespace CarWash.Application.Agendamentos.Common;

/// <summary>
/// Indica que o agendamento não pode ser editado devido ao seu status atual
/// (RF010). Herda de <see cref="ConflictException"/> para produzir HTTP 409
/// com slug estável no middleware global. Usada quando o status é
/// <c>Finalizado</c>, <c>Cancelado</c> ou <c>EmAndamento</c>.
/// </summary>
public sealed class EdicaoBloqueadaException : ConflictException
{
	public const string SlugPadrao = "agendamento-edicao-bloqueada";

	public const string MensagemFinalizado =
		"Agendamento finalizado não pode ser editado.";

	public const string MensagemCancelado =
		"Agendamento cancelado não pode ser editado.";

	public const string MensagemEmAndamento =
		"Agendamento no status atual não permite edição.";

	public EdicaoBloqueadaException(string mensagem, string motivoStatus)
		: base(mensagem, SlugPadrao)
	{
		MotivoStatus = motivoStatus;
	}

	public string MotivoStatus { get; }
}
