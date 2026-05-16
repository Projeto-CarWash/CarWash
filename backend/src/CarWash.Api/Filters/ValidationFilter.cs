using FluentValidation;
using ValidationException = CarWash.Application.Common.Exceptions.ValidationException;

namespace CarWash.Api.Filters;

/// <summary>
/// Filtro genérico que executa o(s) <see cref="IValidator{T}"/> registrado(s) para o
/// argumento de tipo <typeparamref name="T"/> antes do handler. Falhas viram
/// <see cref="ValidationException"/> e são traduzidas para 400 pelo middleware global.
/// </summary>
public sealed class ValidationFilter<T> : IEndpointFilter
    where T : class
{
    private readonly IValidator<T> _validator;

    public ValidationFilter(IValidator<T> validator)
    {
        _validator = validator;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var argumento = context.Arguments.OfType<T>().FirstOrDefault();
        if (argumento is null)
        {
            throw new ValidationException(
                "Dados do usuário inválidos. Verifique os campos e tente novamente.",
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["body"] = ["Corpo da requisição ausente ou malformado."],
                });
        }

        var resultado = await _validator.ValidateAsync(argumento, context.HttpContext.RequestAborted).ConfigureAwait(false);
        if (!resultado.IsValid)
        {
            var erros = resultado.Errors
                .GroupBy(e => NormalizarCampo(e.PropertyName), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).Distinct(StringComparer.Ordinal).ToArray(),
                    StringComparer.OrdinalIgnoreCase);

            throw new ValidationException(
                "Dados do usuário inválidos. Verifique os campos e tente novamente.",
                erros);
        }

        return await next(context).ConfigureAwait(false);
    }

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
