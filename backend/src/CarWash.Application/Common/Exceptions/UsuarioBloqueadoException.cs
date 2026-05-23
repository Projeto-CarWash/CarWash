namespace CarWash.Application.Common.Exceptions;

/// <summary>
/// Lockout temporário do usuário após exceder o limite de tentativas inválidas
/// consecutivas (RF001). Mapeada para HTTP 403 + <c>ProblemDetails</c> no middleware
/// global, expondo <c>bloqueadoAte</c> (ISO 8601 UTC) como extension property para
/// o frontend exibir countdown.
/// </summary>
public sealed class UsuarioBloqueadoException : Exception
{
    public const string MensagemPadrao =
        "Acesso temporariamente bloqueado por tentativas inválidas. Tente novamente em alguns minutos.";

    public UsuarioBloqueadoException()
        : base(MensagemPadrao)
    {
    }

    public UsuarioBloqueadoException(string message)
        : base(message)
    {
    }

    public UsuarioBloqueadoException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public UsuarioBloqueadoException(DateTime bloqueadoAte)
        : base(MensagemPadrao)
    {
        BloqueadoAte = bloqueadoAte;
    }

    public UsuarioBloqueadoException(DateTime bloqueadoAte, string mensagem)
        : base(mensagem)
    {
        BloqueadoAte = bloqueadoAte;
    }

    public UsuarioBloqueadoException(DateTime bloqueadoAte, string mensagem, Exception innerException)
        : base(mensagem, innerException)
    {
        BloqueadoAte = bloqueadoAte;
    }

    /// <summary>Instante (UTC) em que o bloqueio expira.</summary>
    public DateTime BloqueadoAte { get; }
}
