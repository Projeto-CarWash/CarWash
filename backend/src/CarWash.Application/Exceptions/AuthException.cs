namespace CarWash.Application.Exceptions;

/// <summary>
/// Exceção personalizada para falhas de autenticação.
/// </summary>
public class AuthException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthException"/> class.
    /// </summary>
    /// <param name="statusCode">O código de status HTTP.</param>
    /// <param name="errorCode">O código interno do erro.</param>
    /// <param name="message">A mensagem descritiva.</param>
    public AuthException(int statusCode, string errorCode, string message)
        : base(message)
    {
        this.StatusCode = statusCode;
        this.ErrorCode = errorCode;
    }

    /// <summary>
    /// Gets o código de status HTTP retornado.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Gets o código de erro interno do sistema.
    /// </summary>
    public string ErrorCode { get; }
}
