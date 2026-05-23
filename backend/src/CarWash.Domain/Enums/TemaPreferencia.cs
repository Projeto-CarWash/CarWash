namespace CarWash.Domain.Enums;

/// <summary>
/// Tema da interface (RF016) — CHECK <c>ck_pref_tema</c>.
/// </summary>
public enum TemaPreferencia
{
    Claro,
    Escuro,
}

public static class TemaPreferenciaExtensions
{
    public static string ToDbValue(this TemaPreferencia tema) => tema switch
    {
        TemaPreferencia.Claro => "claro",
        TemaPreferencia.Escuro => "escuro",
        _ => throw new ArgumentOutOfRangeException(nameof(tema), tema, "Tema desconhecido."),
    };

    public static TemaPreferencia FromDbValue(string raw) => raw switch
    {
        "claro" => TemaPreferencia.Claro,
        "escuro" => TemaPreferencia.Escuro,
        _ => throw new ArgumentOutOfRangeException(nameof(raw), raw, "Tema persistido inválido."),
    };
}
