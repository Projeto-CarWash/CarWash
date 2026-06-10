using System.Linq;

namespace CarWash.Application.Common;

/// <summary>
/// Mascara documentos (CPF/CNPJ) para exibição em payloads de resposta sem
/// expor o número completo. CPF mantém os 3 primeiros dígitos e o DV; CNPJ
/// mantém os 2 primeiros dígitos e o DV.
/// </summary>
public static class DocumentoMasker
{
    /// <summary>
    /// Retorna o documento mascarado: CPF "123.456.789-00" → "123.***.***-00";
    /// CNPJ "12.345.678/0001-99" → "12.***.***/****-99". Documentos vazios são
    /// devolvidos inalterados; tamanhos inesperados viram apenas asteriscos.
    /// </summary>
    /// <returns></returns>
    public static string Mascarar(string documento)
    {
        if (string.IsNullOrWhiteSpace(documento))
        {
            return documento;
        }

        string digitos = new(documento.Where(char.IsDigit).ToArray());

        if (digitos.Length == 11)
        {
            return $"{digitos[..3]}.***.***-{digitos[^2..]}";
        }

        if (digitos.Length == 14)
        {
            return $"{digitos[..2]}.***.***/****-{digitos[^2..]}";
        }

        return new string('*', documento.Length);
    }
}
