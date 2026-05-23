using CarWash.Application.Agendamentos.Common;

namespace CarWash.Application.Agendamentos.PreConfirmar;

/// <summary>
/// Resposta da pré-confirmação (RF015): o resumo de revisão, o
/// <c>tokenConfirmacao</c> assinado (15 min) e o instante de expiração. Nada foi
/// persistido — a confirmação efetiva acontece em <c>POST .../confirmar</c>.
/// </summary>
public sealed class PreConfirmacaoResponse
{
    /// <summary>Token assinado a ser reenviado na confirmação.</summary>
    public string TokenConfirmacao { get; init; } = string.Empty;

    /// <summary>Instante (UTC) a partir do qual o token deixa de ser aceito.</summary>
    public DateTime ExpiraEm { get; init; }

    /// <summary>Resumo de negócio para revisão pelo usuário.</summary>
    public ResumoConfirmacaoResponse Resumo { get; init; } = new();

    public string TraceId { get; init; } = string.Empty;
}
