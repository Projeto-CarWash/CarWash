using CarWash.Application.Common.Exceptions;

namespace CarWash.Application.Filiais.Common;

/// <summary>
/// Indica que o nome informado já está em uso por outra filial — comparação
/// case-insensitive via UK funcional <c>uk_filiais_nome_lower</c>. Herda de
/// <see cref="ConflictException"/> para reaproveitar status 409 + slug no
/// middleware global.
/// </summary>
#pragma warning disable RCS1194 // Exceção semântica; construtores cobrem os usos reais.
public sealed class FilialNomeJaExisteException : ConflictException
#pragma warning restore RCS1194
{
    public const string MensagemPadrao = "Já existe filial cadastrada com este nome.";
    public const string SlugPadrao = "filial-nome-ja-existe";

    public FilialNomeJaExisteException()
        : base(MensagemPadrao, SlugPadrao)
    {
    }

    public FilialNomeJaExisteException(Exception innerException)
        : base(MensagemPadrao, SlugPadrao, innerException)
    {
    }

    public FilialNomeJaExisteException(string mensagem)
        : base(mensagem, SlugPadrao)
    {
    }

    public FilialNomeJaExisteException(string mensagem, Exception innerException)
        : base(mensagem, SlugPadrao, innerException)
    {
    }
}
