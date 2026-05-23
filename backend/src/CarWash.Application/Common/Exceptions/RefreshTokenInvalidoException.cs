namespace CarWash.Application.Common.Exceptions;

/// <summary>
/// Refresh token ausente, expirado ou revogado. Mapeado para HTTP 401 pelo
/// <c>ExceptionHandlingMiddleware</c> — o cliente deve redirecionar para /login.
/// </summary>
public class RefreshTokenInvalidoException : Exception
{
    public RefreshTokenInvalidoException()
        : base("Refresh token inválido ou expirado.")
    {
    }

    protected RefreshTokenInvalidoException(string mensagem)
        : base(mensagem)
    {
    }
}
