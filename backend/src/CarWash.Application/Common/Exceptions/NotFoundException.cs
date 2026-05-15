namespace CarWash.Application.Common.Exceptions;

/// <summary>
/// Recurso solicitado não foi encontrado. Mapeada para HTTP 404 + <c>ProblemDetails</c>
/// no middleware global.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string mensagem)
        : base(mensagem)
    {
    }

    public NotFoundException()
        : base("Recurso não encontrado.")
    {
    }

    public NotFoundException(string mensagem, Exception innerException)
        : base(mensagem, innerException)
    {
    }
}
