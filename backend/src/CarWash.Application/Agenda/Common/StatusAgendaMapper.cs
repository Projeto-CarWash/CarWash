namespace CarWash.Application.Agenda.Common;

/// <summary>
/// Mapeador estático entre os 4 status do contrato da API (RF009 / card 132) e
/// os status persistidos no domínio:
/// <list type="bullet">
///   <item><c>AGENDADO</c> ↔ <c>agendado</c>.</item>
///   <item><c>EM_ANDAMENTO</c> ↔ <c>em_andamento</c> (atendimento iniciado — RF010/RF013).</item>
///   <item><c>CONCLUIDO</c> ↔ <c>finalizado</c> (alias do contrato).</item>
///   <item><c>CANCELADO</c> ↔ <c>cancelado</c>.</item>
/// </list>
/// </summary>
public static class StatusAgendaMapper
{
    /// <summary>Status do contrato da API para atendimento em execução.</summary>
    public const string EmAndamento = "EM_ANDAMENTO";

    private static readonly Dictionary<string, string> ApiParaDb =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["AGENDADO"] = "agendado",
            [EmAndamento] = "em_andamento",
            ["CONCLUIDO"] = "finalizado",
            ["CANCELADO"] = "cancelado",
        };

    private static readonly Dictionary<string, string> DbParaApi =
        new(StringComparer.Ordinal)
        {
            ["agendado"] = "AGENDADO",
            ["em_andamento"] = EmAndamento,
            ["finalizado"] = "CONCLUIDO",
            ["cancelado"] = "CANCELADO",
        };

    /// <summary>
    /// Gets os 4 valores aceitos como filtro de <c>status</c> na query da agenda.
    /// </summary>
    public static IReadOnlyCollection<string> StatusValidosApi { get; } =
        new[] { "AGENDADO", EmAndamento, "CONCLUIDO", "CANCELADO" };

    /// <summary>
    /// Indica se <paramref name="statusApi"/> é um dos 4 valores aceitos no
    /// contrato (case-insensitive). Usado pelo validator.
    /// </summary>
    /// <returns></returns>
    public static bool EhStatusApiValido(string? statusApi)
    {
        if (string.IsNullOrWhiteSpace(statusApi))
        {
            return false;
        }

        return StatusValidosApi.Contains(statusApi.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Indica se <paramref name="statusApi"/> é o valor <c>EM_ANDAMENTO</c>.
    /// Histórico: na ADR 0004 (L1) o status não existia persistido e a agenda
    /// curto-circuitava para lista vazia; com o ciclo iniciar/finalizar
    /// (RF010/RF013) o filtro passou a resolver normalmente no banco.
    /// </summary>
    /// <returns></returns>
    public static bool EhEmAndamento(string? statusApi) =>
        !string.IsNullOrWhiteSpace(statusApi)
        && string.Equals(statusApi.Trim(), EmAndamento, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Converte um status do contrato da API para o valor persistido no banco.
    /// Retorna <c>null</c> para valores fora do contrato.
    /// </summary>
    /// <returns></returns>
    public static string? ParaDb(string? statusApi)
    {
        if (string.IsNullOrWhiteSpace(statusApi))
        {
            return null;
        }

        return ApiParaDb.TryGetValue(statusApi.Trim(), out string? db) ? db : null;
    }

    /// <summary>
    /// Converte um status persistido no banco para o valor uppercase do contrato
    /// da API. Sempre serializado dessa forma na resposta (ADR 0004).
    /// </summary>
    /// <returns></returns>
    public static string ParaApi(string statusDb)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(statusDb);

        return DbParaApi.TryGetValue(statusDb, out string? api)
            ? api
            : throw new ArgumentOutOfRangeException(nameof(statusDb), statusDb, "Status persistido inválido.");
    }
}
