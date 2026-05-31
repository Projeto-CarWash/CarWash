namespace CarWash.Application.Common.Exceptions;

/// <summary>
/// Conflito de estado (ex.: chave única violada, recurso já existente).
/// Mapeada para HTTP 409 + <c>ProblemDetails</c> no middleware global.
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string mensagem)
        : base(mensagem)
    {
        Slug = "conflict";
    }

    public ConflictException(string mensagem, string slug)
        : base(mensagem)
    {
        Slug = slug;
    }

    public ConflictException(string mensagem, string slug, Exception innerException)
        : base(mensagem, innerException)
    {
        Slug = slug;
    }

    public ConflictException()
        : base("Conflito ao persistir o recurso.")
    {
        Slug = "conflict";
    }

    public ConflictException(string mensagem, Exception innerException)
        : base(mensagem, innerException)
    {
        Slug = "conflict";
    }

    /// <summary>
    /// Gets identificador curto do conflito, usado no campo <c>type</c> do ProblemDetails.
    /// </summary>
    public string Slug { get; }
}
