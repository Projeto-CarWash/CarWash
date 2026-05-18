namespace CarWash.Domain.Enums;

/// <summary>
/// Eventos do histórico de agendamento (CHECK <c>ck_hist_evento</c>, RN007).
/// </summary>
public enum EventoHistorico
{
    Criado,
    Editado,
    Cancelado,
    Finalizado,
}

public static class EventoHistoricoExtensions
{
    public static string ToDbValue(this EventoHistorico evento) => evento switch
    {
        EventoHistorico.Criado => "CRIADO",
        EventoHistorico.Editado => "EDITADO",
        EventoHistorico.Cancelado => "CANCELADO",
        EventoHistorico.Finalizado => "FINALIZADO",
        _ => throw new ArgumentOutOfRangeException(nameof(evento), evento, "Evento desconhecido."),
    };

    public static EventoHistorico FromDbValue(string raw) => raw switch
    {
        "CRIADO" => EventoHistorico.Criado,
        "EDITADO" => EventoHistorico.Editado,
        "CANCELADO" => EventoHistorico.Cancelado,
        "FINALIZADO" => EventoHistorico.Finalizado,
        _ => throw new ArgumentOutOfRangeException(nameof(raw), raw, "Evento persistido inválido."),
    };
}
