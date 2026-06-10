namespace CarWash.Domain.Enums;

public enum StatusAgendamento
{
    Agendado,
    EmAndamento,
    Cancelado,
    Finalizado,
}

public static class StatusAgendamentoExtensions
{
    public static string ToDbValue(this StatusAgendamento status) => status switch
    {
        StatusAgendamento.Agendado => "agendado",
        StatusAgendamento.EmAndamento => "em_andamento",
        StatusAgendamento.Cancelado => "cancelado",
        StatusAgendamento.Finalizado => "finalizado",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Status desconhecido."),
    };

    public static StatusAgendamento FromDbValue(string raw) => raw switch
    {
        "agendado" => StatusAgendamento.Agendado,
        "em_andamento" => StatusAgendamento.EmAndamento,
        "cancelado" => StatusAgendamento.Cancelado,
        "finalizado" => StatusAgendamento.Finalizado,
        _ => throw new ArgumentOutOfRangeException(nameof(raw), raw, "Status persistido inválido."),
    };

    public static bool ConsomeCapacidade(this StatusAgendamento status) =>
        status is StatusAgendamento.Agendado or StatusAgendamento.EmAndamento;
}
