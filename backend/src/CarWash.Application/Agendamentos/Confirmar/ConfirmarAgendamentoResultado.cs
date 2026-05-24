using CarWash.Application.Agendamentos.Common;

namespace CarWash.Application.Agendamentos.Confirmar;

/// <summary>
/// Wrapper do resultado da confirmação (RF015). Carrega o
/// <see cref="AgendamentoResponse"/> (o mesmo contrato do RF007) e a flag
/// <see cref="EhReplay"/>, que o endpoint usa para adicionar o header
/// <c>Idempotent-Replay: true</c> quando a resposta veio de um registro de
/// idempotência já existente.
/// </summary>
public sealed record ConfirmarAgendamentoResultado(AgendamentoResponse Agendamento, bool EhReplay)
{
    /// <summary>Confirmação persistida agora.</summary>
    public static ConfirmarAgendamentoResultado Novo(AgendamentoResponse agendamento) =>
        new(agendamento, false);

    /// <summary>Replay idempotente — resposta original devolvida.</summary>
    public static ConfirmarAgendamentoResultado Replay(AgendamentoResponse agendamento) =>
        new(agendamento, true);
}
