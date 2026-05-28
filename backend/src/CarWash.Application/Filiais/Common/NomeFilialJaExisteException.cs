using CarWash.Application.Common.Exceptions;

namespace CarWash.Application.Filiais.Common;

/// <summary>
/// Indica que já existe uma filial cadastrada com o nome informado (UK
/// <c>uk_filiais_nome</c>). Disparada pelo <see cref="Persistence.IFilialRepository"/>
/// quando o banco detecta a violação em concorrência. Herda de
/// <see cref="ConflictException"/> — reutiliza o slug + status 409 no middleware
/// global, sem exigir tratamento adicional.
/// </summary>
#pragma warning disable RCS1194 // Exceção semântica; os construtores cobrem os usos reais (sem stacktrace de Exception(string,Exception) sozinho).
public sealed class NomeFilialJaExisteException : ConflictException
#pragma warning restore RCS1194
{
    public const string MensagemPadrao = "Já existe uma filial cadastrada com este nome.";
    public const string SlugPadrao = "filial-nome-duplicado";

    public NomeFilialJaExisteException()
        : base(MensagemPadrao, SlugPadrao)
    {
    }

    public NomeFilialJaExisteException(Exception innerException)
        : base(MensagemPadrao, SlugPadrao, innerException)
    {
    }

    public NomeFilialJaExisteException(string mensagem)
        : base(mensagem, SlugPadrao)
    {
    }

    public NomeFilialJaExisteException(string mensagem, Exception innerException)
        : base(mensagem, SlugPadrao, innerException)
    {
    }
}
