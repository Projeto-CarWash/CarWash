namespace CarWash.Application.Common.Exceptions;

/// <summary>
/// Falha de autenticação: credenciais inválidas (e-mail inexistente OU senha errada).
/// A mensagem é unificada para evitar enumeração de usuários. Mapeada para HTTP 401 +
/// <c>ProblemDetails</c> no middleware global.
/// </summary>
public sealed class InvalidCredentialsException : Exception
{
    public const string MensagemPadrao = "Usuário ou senha inválidos.";

    public InvalidCredentialsException()
        : base(MensagemPadrao)
    {
    }

    public InvalidCredentialsException(string mensagem)
        : base(mensagem)
    {
    }

    public InvalidCredentialsException(string mensagem, Exception innerException)
        : base(mensagem, innerException)
    {
    }
}
