using CarWash.Application.Common.Exceptions;

namespace CarWash.Application.Usuarios.Common;

/// <summary>
/// Indica que o e-mail informado já está em uso por outro usuário. Disparada
/// pelo <see cref="Persistence.IUsuarioRepository"/> quando o banco detecta a
/// violação da UK <c>uk_usuarios_email</c> em concorrência. Herda de
/// <see cref="ConflictException"/> para reaproveitar o slug + status 409
/// no middleware global, sem exigir tratamento adicional no handler.
/// </summary>
#pragma warning disable RCS1194 // Exceção semântica; os 4 construtores cobrem os usos reais (sem stacktrace de Exception(string,Exception)).
public sealed class EmailJaExisteException : ConflictException
#pragma warning restore RCS1194
{
    public const string MensagemPadrao = "Já existe usuário cadastrado com este e-mail.";
    public const string SlugPadrao = "email-already-exists";

    public EmailJaExisteException()
        : base(MensagemPadrao, SlugPadrao)
    {
    }

    public EmailJaExisteException(Exception innerException)
        : base(MensagemPadrao, SlugPadrao, innerException)
    {
    }

    public EmailJaExisteException(string mensagem, Exception innerException)
        : base(mensagem, SlugPadrao, innerException)
    {
    }

    public EmailJaExisteException(string mensagem)
        : base(mensagem, SlugPadrao)
    {
    }
}
