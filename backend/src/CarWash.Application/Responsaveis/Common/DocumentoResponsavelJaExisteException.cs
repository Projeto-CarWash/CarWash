using CarWash.Application.Common.Exceptions;

namespace CarWash.Application.Responsaveis.Common;

#pragma warning disable RCS1194 // Exceção semântica; construtores cobrem os usos reais.
public sealed class DocumentoResponsavelJaExisteException : ConflictException
#pragma warning restore RCS1194
{
    public const string MensagemPadrao = "Já existe responsável cadastrado com este documento.";
    public const string SlugPadrao = "responsavel-documento-duplicado";

    public DocumentoResponsavelJaExisteException()
        : base(MensagemPadrao, SlugPadrao)
    {
    }

    public DocumentoResponsavelJaExisteException(Exception innerException)
        : base(MensagemPadrao, SlugPadrao, innerException)
    {
    }

    public DocumentoResponsavelJaExisteException(string mensagem, Exception innerException)
        : base(mensagem, SlugPadrao, innerException)
    {
    }

    public DocumentoResponsavelJaExisteException(string mensagem)
        : base(mensagem, SlugPadrao)
    {
    }
}
