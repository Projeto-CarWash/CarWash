namespace CarWash.Application.Agenda.Common;

/// <summary>
/// Mapeador estático entre os 4 status do contrato da API (RF009 / card 132) e
/// os 3 status reais do domínio persistido. Decisão da ADR 0004 (L1):
/// <list type="bullet">
///   <item><c>AGENDADO</c> ↔ <c>agendado</c>.</item>
///   <item><c>CONCLUIDO</c> ↔ <c>finalizado</c> (alias do contrato).</item>
///   <item><c>CANCELADO</c> ↔ <c>cancelado</c>.</item>
///   <item><c>EM_ANDAMENTO</c> — valor de contrato sem correspondente persistido;
///   é filtro válido (não causa 400) mas sempre resolve para conjunto vazio.</item>
/// </list>
/// </summary>
public static class StatusAgendaMapper
{
    /// <summary>Status do contrato da API que não tem correspondente no domínio.</summary>
    public const string EmAndamento = "EM_ANDAMENTO";

    private static readonly Dictionary<string, string> ApiParaDb =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["AGENDADO"] = "agendado",
            ["CONCLUIDO"] = "finalizado",
            ["CANCELADO"] = "cancelado",
        };

    private static readonly Dictionary<string, string> DbParaApi =
        new(StringComparer.Ordinal)
        {
            ["agendado"] = "AGENDADO",
            ["finalizado"] = "CONCLUIDO",
            ["cancelado"] = "CANCELADO",
        };

    /// <summary>
    /// Os 4 valores aceitos como filtro de <c>status</c> na query da agenda.
    /// </summary>
    public static IReadOnlyCollection<string> StatusValidosApi { get; } =
        new[] { "AGENDADO", EmAndamento, "CONCLUIDO", "CANCELADO" };

    /// <summary>
    /// Indica se <paramref name="statusApi"/> é um dos 4 valores aceitos no
    /// contrato (case-insensitive). Usado pelo validator.
    /// </summary>
    public static bool EhStatusApiValido(string? statusApi)
    {
        if (string.IsNullOrWhiteSpace(statusApi))
        {
            return false;
        }

        return StatusValidosApi.Contains(statusApi.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Indica se <paramref name="statusApi"/> é o valor <c>EM_ANDAMENTO</c>, que
    /// curto-circuita a consulta para uma lista vazia (ADR 0004 — L1).
    /// </summary>
    public static bool EhEmAndamento(string? statusApi) =>
        !string.IsNullOrWhiteSpace(statusApi)
        && string.Equals(statusApi.Trim(), EmAndamento, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Converte um status do contrato da API para o valor persistido no banco.
    /// Retorna <c>null</c> para <c>EM_ANDAMENTO</c> (curto-circuito) ou para
    /// valores fora do contrato.
    /// </summary>
    public static string? ParaDb(string? statusApi)
    {
        if (string.IsNullOrWhiteSpace(statusApi))
        {
            return null;
        }

        return ApiParaDb.TryGetValue(statusApi.Trim(), out var db) ? db : null;
    }

    /// <summary>
    /// Converte um status persistido no banco para o valor uppercase do contrato
    /// da API. Sempre serializado dessa forma na resposta (ADR 0004).
    /// </summary>
    public static string ParaApi(string statusDb)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(statusDb);

        return DbParaApi.TryGetValue(statusDb, out var api)
            ? api
            : throw new ArgumentOutOfRangeException(nameof(statusDb), statusDb, "Status persistido inválido.");
    }
}
