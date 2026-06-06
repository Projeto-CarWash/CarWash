namespace CarWash.Application.Agendamentos.Common;

/// <summary>
/// Motivos estruturados de falha na validação de filial (RF019), registrados em
/// <c>audit_logs</c>/log de aplicação. NUNCA são expostos na resposta HTTP —
/// servem apenas à observabilidade (DAT §9.1).
/// </summary>
public static class MotivosFalhaFilial
{
    public const string Ausente = "filial_ausente";
    public const string Invalida = "filial_invalida";
    public const string Inexistente = "filial_inexistente";
    public const string Inativa = "filial_inativa";
}
