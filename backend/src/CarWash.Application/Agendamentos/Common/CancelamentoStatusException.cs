using CarWash.Application.Common.Exceptions;

namespace CarWash.Application.Agendamentos.Common;

/// <summary>
/// Indica que o agendamento não pode ser cancelado devido ao seu status atual
/// (RF010). Herda de <see cref="ConflictException"/> para produzir HTTP 409
/// com slug estável no middleware global. Usada quando o status é
/// <c>Finalizado</c>, <c>Cancelado</c> ou <c>EmAndamento</c>.
/// </summary>
public sealed class CancelamentoStatusException : ConflictException
{
    public const string SlugPadrao = "agendamento-cancelamento-status";

    public const string MensagemFinalizado =
        "Agendamento finalizado não pode ser cancelado.";

    public const string MensagemCancelado =
        "Agendamento já cancelado não pode ser cancelado novamente.";

    public const string MensagemEmAndamento =
        "Agendamento em andamento não pode ser cancelado.";

    public CancelamentoStatusException(string mensagem)
        : base(mensagem, SlugPadrao)
    {
    }
}
