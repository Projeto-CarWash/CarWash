namespace CarWash.Domain.Enums;

/// <summary>
/// Status do <c>outbox_eventos</c> — CHECK <c>ck_outbox_status</c>.
/// </summary>
public enum StatusOutbox
{
    Pendente,
    Processando,
    Processado,
    Falha,
}

public static class StatusOutboxExtensions
{
    public static string ToDbValue(this StatusOutbox status) => status switch
    {
        StatusOutbox.Pendente => "pendente",
        StatusOutbox.Processando => "processando",
        StatusOutbox.Processado => "processado",
        StatusOutbox.Falha => "falha",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Status desconhecido."),
    };

    public static StatusOutbox FromDbValue(string raw) => raw switch
    {
        "pendente" => StatusOutbox.Pendente,
        "processando" => StatusOutbox.Processando,
        "processado" => StatusOutbox.Processado,
        "falha" => StatusOutbox.Falha,
        _ => throw new ArgumentOutOfRangeException(nameof(raw), raw, "Status persistido inválido."),
    };
}
