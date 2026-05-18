namespace CarWash.Domain.Enums;

/// <summary>
/// Estados possíveis de um agendamento — também presentes em <c>ck_ag_status</c> (RN004/RN006).
/// </summary>
public enum StatusAgendamento
{
    Agendado,
    Cancelado,
    Finalizado,
}

public static class StatusAgendamentoExtensions
{
    public static string ToDbValue(this StatusAgendamento status) => status switch
    {
        StatusAgendamento.Agendado => "agendado",
        StatusAgendamento.Cancelado => "cancelado",
        StatusAgendamento.Finalizado => "finalizado",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Status desconhecido."),
    };

    public static StatusAgendamento FromDbValue(string raw) => raw switch
    {
        "agendado" => StatusAgendamento.Agendado,
        "cancelado" => StatusAgendamento.Cancelado,
        "finalizado" => StatusAgendamento.Finalizado,
        _ => throw new ArgumentOutOfRangeException(nameof(raw), raw, "Status persistido inválido."),
    };
}
