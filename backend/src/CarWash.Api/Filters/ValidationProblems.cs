using FluentValidation.Results;
using ValidationException = CarWash.Application.Common.Exceptions.ValidationException;

namespace CarWash.Api.Filters;

/// <summary>
/// Conjunto de helpers reutilizados pelo <see cref="ValidationFilter{T}"/> e
/// pelos endpoints que precisam validar inline (composição de id de rota +
/// body num command). Mantém o formato do payload de erro consistente entre
/// os pontos: chaves em camelCase, valor <c>body</c> para erros sem propriedade,
/// mensagens deduplicadas.
/// </summary>
public static class ValidationProblems
{
    /// <summary>
    /// Lança <see cref="ValidationException"/> com o mapa de erros agrupado por
    /// campo, caso <paramref name="resultado"/> seja inválido. No-op em sucesso.
    /// </summary>
    public static void EnsureValid(ValidationResult resultado, string mensagem)
    {
        ArgumentNullException.ThrowIfNull(resultado);

        if (resultado.IsValid)
        {
            return;
        }

        throw new ValidationException(mensagem, AgruparErros(resultado));
    }

    /// <summary>
    /// Lança <see cref="ValidationException"/> com <c>body</c> mapeado para a
    /// mensagem informada. Usado quando o request chega <c>null</c>/malformado.
    /// </summary>
    public static ValidationException BodyAusente(string mensagem, string detalhe)
    {
        return new ValidationException(
            mensagem,
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["body"] = [detalhe],
            });
    }

    private static Dictionary<string, string[]> AgruparErros(ValidationResult resultado) =>
        resultado.Errors
            .GroupBy(e => NormalizarCampo(e.PropertyName), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).Distinct(StringComparer.Ordinal).ToArray(),
                StringComparer.OrdinalIgnoreCase);

    private static string NormalizarCampo(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return "body";
        }

        // FluentValidation usa PascalCase; mapear para camelCase no payload de erro.
        return char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
    }
}
