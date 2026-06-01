using CarWash.Application.Common.Exceptions;

namespace CarWash.Application.Filiais.Common;

/// <summary>
/// Indica que o CNPJ informado já está em uso por outra filial
/// (UK parcial <c>uk_filiais_cnpj</c>). Herda de <see cref="ConflictException"/>
/// para reaproveitar status 409 + slug no middleware global.
/// </summary>
#pragma warning disable RCS1194 // Exceção semântica; construtores cobrem os usos reais.
public sealed class FilialCnpjJaExisteException : ConflictException
#pragma warning restore RCS1194
{
    public const string MensagemPadrao = "Já existe filial cadastrada com este CNPJ.";
    public const string SlugPadrao = "filial-cnpj-ja-existe";

    public FilialCnpjJaExisteException()
        : base(MensagemPadrao, SlugPadrao)
    {
    }

    public FilialCnpjJaExisteException(Exception innerException)
        : base(MensagemPadrao, SlugPadrao, innerException)
    {
    }

    public FilialCnpjJaExisteException(string mensagem)
        : base(mensagem, SlugPadrao)
    {
    }

    public FilialCnpjJaExisteException(string mensagem, Exception innerException)
        : base(mensagem, SlugPadrao, innerException)
    {
    }
}
