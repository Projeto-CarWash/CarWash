using System.Collections.ObjectModel;

namespace CarWash.Application.Common.Exceptions;

/// <summary>
/// Falha de validação de entrada (FluentValidation ou regra de aplicação).
/// Mapeada para HTTP 400 + <c>ProblemDetails</c> no middleware global.
/// Não-selada para permitir especializações com 400 (ex.:
/// <c>TokenConfirmacaoInvalidoException</c>) sem novo <c>catch</c> no middleware.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string mensagem, IReadOnlyDictionary<string, string[]> erros)
        : base(mensagem)
    {
        ArgumentNullException.ThrowIfNull(erros);
        var copia = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in erros)
        {
            copia[kvp.Key] = kvp.Value;
        }

        Erros = new ReadOnlyDictionary<string, string[]>(copia);
    }

    public ValidationException(string mensagem)
        : base(mensagem)
    {
        Erros = new ReadOnlyDictionary<string, string[]>(new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase));
    }

    public ValidationException()
        : this("Dados do usuário inválidos. Verifique os campos e tente novamente.")
    {
    }

    public ValidationException(string mensagem, Exception innerException)
        : base(mensagem, innerException)
    {
        Erros = new ReadOnlyDictionary<string, string[]>(new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyDictionary<string, string[]> Erros { get; }
}
