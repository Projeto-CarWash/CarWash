namespace CarWash.Domain.Enums;

public enum GrauVinculo
{
    ResponsavelFinanceiro,
    ResponsavelLegal,
    Procurador,
    Conjuge,
    PaiMae,
    Outro,
}

public static class GrauVinculoExtensions
{
    public static string ToDbValue(this GrauVinculo grau) => grau switch
    {
        GrauVinculo.ResponsavelFinanceiro => "RESPONSAVEL_FINANCEIRO",
        GrauVinculo.ResponsavelLegal => "RESPONSAVEL_LEGAL",
        GrauVinculo.Procurador => "PROCURADOR",
        GrauVinculo.Conjuge => "CONJUGE",
        GrauVinculo.PaiMae => "PAI_MAE",
        GrauVinculo.Outro => "OUTRO",
        _ => throw new ArgumentOutOfRangeException(nameof(grau), grau, "Grau de vínculo desconhecido."),
    };

    public static GrauVinculo FromDbValue(string raw) => raw switch
    {
        "RESPONSAVEL_FINANCEIRO" => GrauVinculo.ResponsavelFinanceiro,
        "RESPONSAVEL_LEGAL" => GrauVinculo.ResponsavelLegal,
        "PROCURADOR" => GrauVinculo.Procurador,
        "CONJUGE" => GrauVinculo.Conjuge,
        "PAI_MAE" => GrauVinculo.PaiMae,
        "OUTRO" => GrauVinculo.Outro,
        _ => throw new ArgumentOutOfRangeException(nameof(raw), raw, "Grau de vínculo persistido inválido."),
    };

    public static bool IsValidDbValue(string raw) => raw switch
    {
        "RESPONSAVEL_FINANCEIRO" => true,
        "RESPONSAVEL_LEGAL" => true,
        "PROCURADOR" => true,
        "CONJUGE" => true,
        "PAI_MAE" => true,
        "OUTRO" => true,
        _ => false,
    };
}
