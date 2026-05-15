namespace CarWash.Application.Common.Exceptions;

/// <summary>
/// Credenciais válidas, porém o usuário está inativo. Mapeada para HTTP 403 +
/// <c>ProblemDetails</c> no middleware global.
/// </summary>
public sealed class UsuarioInativoException : Exception
{
    public const string MensagemPadrao = "Acesso bloqueado. Usuário inativo.";

    public UsuarioInativoException()
        : base(MensagemPadrao)
    {
    }

    public UsuarioInativoException(string mensagem)
        : base(mensagem)
    {
    }

    public UsuarioInativoException(string mensagem, Exception innerException)
        : base(mensagem, innerException)
    {
    }
}
