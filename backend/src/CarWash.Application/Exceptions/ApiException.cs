using System.Collections.Generic;

namespace CarWash.Application.Exceptions;

/// <summary>
/// Excecao generica para erros de API.
/// </summary>
public class ApiException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiException"/> class.
    /// </summary>
    public ApiException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiException"/> class.
    /// </summary>
    /// <param name="message">Mensagem da excecao.</param>
    public ApiException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiException"/> class.
    /// </summary>
    /// <param name="message">Mensagem da excecao.</param>
    /// <param name="innerException">Excecao interna.</param>
    public ApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiException"/> class.
    /// </summary>
    /// <param name="statusCode">O codigo de status HTTP.</param>
    /// <param name="errorCode">O codigo interno do erro.</param>
    /// <param name="message">A mensagem descritiva.</param>
    /// <param name="errors">Erros detalhados por campo.</param>
    public ApiException(
        int statusCode,
        string errorCode,
        string message,
        Dictionary<string, string[]>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        Errors = errors;
    }

    /// <summary>
    /// Gets o codigo de status HTTP retornado.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Gets o codigo de erro interno do sistema.
    /// </summary>
    public string ErrorCode { get; } = string.Empty;

    /// <summary>
    /// Gets os erros detalhados por campo.
    /// </summary>
    public Dictionary<string, string[]>? Errors { get; }
}
