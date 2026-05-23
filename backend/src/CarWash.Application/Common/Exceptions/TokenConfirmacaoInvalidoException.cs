namespace CarWash.Application.Common.Exceptions;

/// <summary>
/// Token de confirmação (RF015) com formato, assinatura ou <c>usuarioId</c>
/// inválido. É um caso de entrada malformada — herda <see cref="ValidationException"/>
/// para reaproveitar o mapeamento 400 + <c>ProblemDetails</c> do middleware sem
/// alterá-lo. A distinção para 410 (token válido porém expirado) é feita pela
/// <see cref="SessaoConfirmacaoExpiradaException"/>.
/// </summary>
#pragma warning disable RCS1194 // Exceção semântica; o construtor padrão cobre o uso real.
public sealed class TokenConfirmacaoInvalidoException : ValidationException
#pragma warning restore RCS1194
{
    public const string Mensagem = "Token de confirmação inválido.";

    public const string Campo = "tokenConfirmacao";

    public TokenConfirmacaoInvalidoException()
        : base(Mensagem, MontarErros())
    {
    }

    public TokenConfirmacaoInvalidoException(Exception innerException)
        : base(Mensagem, innerException)
    {
    }

    private static Dictionary<string, string[]> MontarErros() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            [Campo] = [Mensagem],
        };
}
