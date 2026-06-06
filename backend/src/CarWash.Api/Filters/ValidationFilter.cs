using FluentValidation;

namespace CarWash.Api.Filters;

/// <summary>
/// Filtro genérico que executa o(s) <see cref="IValidator{T}"/> registrado(s) para o
/// argumento de tipo <typeparamref name="T"/> antes do handler. Falhas viram
/// <c>ValidationException</c> via <see cref="ValidationProblems"/> e são
/// traduzidas para 400 pelo middleware global.
/// </summary>
public sealed class ValidationFilter<T> : IEndpointFilter
    where T : class
{
    /// <summary>
    /// Mantida idêntica à mensagem histórica usada pelos endpoints de Usuarios
    /// (rotas autenticadas com payload de cadastro). O contrato HTTP afirma essa
    /// string no campo <c>title</c> do ProblemDetails — clientes/tests dependem
    /// dela. Slices que precisem de mensagem própria devem usar validação inline.
    /// </summary>
    private const string MensagemPadrao =
        "Dados do usuário inválidos. Verifique os campos e tente novamente.";

    private readonly IValidator<T> _validator;

    public ValidationFilter(IValidator<T> validator)
    {
        _validator = validator;
    }

    /// <inheritdoc/>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var argumento = context.Arguments.OfType<T>().FirstOrDefault();
        if (argumento is null)
        {
            throw ValidationProblems.BodyAusente(MensagemPadrao, "Corpo da requisição ausente ou malformado.");
        }

        var resultado = await _validator.ValidateAsync(argumento, context.HttpContext.RequestAborted).ConfigureAwait(false);
        ValidationProblems.EnsureValid(resultado, MensagemPadrao);

        return await next(context).ConfigureAwait(false);
    }
}
