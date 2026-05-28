using CarWash.Application.Common.Exceptions;

namespace CarWash.Application.Filiais.Common;

/// <summary>
/// Indica que o código informado já está em uso por outra filial
/// (UK <c>uk_filiais_codigo</c>). Herda de <see cref="ConflictException"/>
/// para reaproveitar status 409 + slug no middleware global.
/// </summary>
#pragma warning disable RCS1194 // Exceção semântica; construtores cobrem os usos reais.
public sealed class FilialCodigoJaExisteException : ConflictException
#pragma warning restore RCS1194
{
    public const string MensagemPadrao = "Já existe filial cadastrada com este código.";
    public const string SlugPadrao = "filial-codigo-ja-existe";

    public FilialCodigoJaExisteException()
        : base(MensagemPadrao, SlugPadrao)
    {
    }

    public FilialCodigoJaExisteException(Exception innerException)
        : base(MensagemPadrao, SlugPadrao, innerException)
    {
    }

    public FilialCodigoJaExisteException(string mensagem)
        : base(mensagem, SlugPadrao)
    {
    }

    public FilialCodigoJaExisteException(string mensagem, Exception innerException)
        : base(mensagem, SlugPadrao, innerException)
    {
    }
}
