namespace CarWash.Domain.Enums;

/// <summary>
/// Tema da interface (RF016) — CHECK ck_pref_tema.
/// </summary>
public enum TemaPreferencia
{
    Light,
    Dark,
}

public static class TemaPreferenciaExtensions
{
    public static string ToDbValue(this TemaPreferencia tema) => tema switch
    {
        TemaPreferencia.Light => "light",
        TemaPreferencia.Dark => "dark",
        _ => throw new ArgumentOutOfRangeException(nameof(tema), tema, "Tema desconhecido."),
    };

    public static TemaPreferencia FromDbValue(string raw) => raw switch
    {
        "light" => TemaPreferencia.Light,
        "dark" => TemaPreferencia.Dark,
        _ => throw new ArgumentOutOfRangeException(nameof(raw), raw, "Tema persistido inválido."),
    };

    public static TemaPreferencia FromApiValue(string raw) => raw?.Trim().ToLowerInvariant() switch
    {
        "light" => TemaPreferencia.Light,
        "dark" => TemaPreferencia.Dark,
        _ => throw new ArgumentOutOfRangeException(nameof(raw), raw, "Tema inválido. Informe light ou dark."),
    };
}
