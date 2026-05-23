using CarWash.Application.Common.Exceptions;

namespace CarWash.Application.Clientes.Common;

/// <summary>
/// Indica que o e-mail informado já está em uso por outro cliente ativo
/// (índice parcial <c>ux_clientes_email</c>). Herda de
/// <see cref="ConflictException"/> para reaproveitar o status 409 + slug no
/// middleware global.
/// </summary>
#pragma warning disable RCS1194 // Exceção semântica; construtores cobrem os usos reais.
public sealed class EmailClienteJaExisteException : ConflictException
#pragma warning restore RCS1194
{
    public const string MensagemPadrao = "Já existe cliente cadastrado com este e-mail.";
    public const string SlugPadrao = "cliente-email-duplicado";

    public EmailClienteJaExisteException()
        : base(MensagemPadrao, SlugPadrao)
    {
    }

    public EmailClienteJaExisteException(Exception innerException)
        : base(MensagemPadrao, SlugPadrao, innerException)
    {
    }

    public EmailClienteJaExisteException(string mensagem, Exception innerException)
        : base(mensagem, SlugPadrao, innerException)
    {
    }

    public EmailClienteJaExisteException(string mensagem)
        : base(mensagem, SlugPadrao)
    {
    }
}
