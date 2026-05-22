namespace CarWash.Application.Common.Exceptions;

/// <summary>
/// Token de confirmação (RF015) com assinatura e formato válidos, porém com
/// <c>exp</c> no passado — a janela de 15 min da pré-confirmação encerrou. Mapeada
/// para HTTP 410 Gone no <c>ExceptionHandlingMiddleware</c>: o recurso (sessão de
/// confirmação) existiu e deixou de existir; o cliente deve gerar nova prévia.
/// Não herda de outra exceção de aplicação porque 410 não tem equivalente.
/// </summary>
#pragma warning disable RCS1194 // Exceção semântica; o construtor padrão cobre o uso real.
public sealed class SessaoConfirmacaoExpiradaException : Exception
#pragma warning restore RCS1194
{
    public const string MensagemPadrao =
        "Sessão de confirmação expirada. Gere uma nova pré-confirmação.";

    public const string Slug = "sessao-confirmacao-expirada";

    public SessaoConfirmacaoExpiradaException()
        : base(MensagemPadrao)
    {
    }

    public SessaoConfirmacaoExpiradaException(string mensagem)
        : base(mensagem)
    {
    }

    public SessaoConfirmacaoExpiradaException(string mensagem, Exception innerException)
        : base(mensagem, innerException)
    {
    }
}
