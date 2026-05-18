using CarWash.Application.Common.Exceptions;

namespace CarWash.Application.Clientes.Common;

/// <summary>
/// Indica que o CPF/CNPJ informado já está em uso por outro cliente. Disparada
/// pelo <see cref="Persistence.IClienteRepository"/> quando o banco detecta a
/// violação de UK (<c>uk_clientes_cpf</c> / <c>uk_clientes_cnpj</c>) em
/// concorrência. Herda de <see cref="ConflictException"/> para reaproveitar o
/// status 409 + slug no middleware global.
/// </summary>
#pragma warning disable RCS1194 // Exceção semântica; construtores cobrem os usos reais.
public sealed class DocumentoClienteJaExisteException : ConflictException
#pragma warning restore RCS1194
{
    public const string MensagemPadrao = "Já existe cliente cadastrado com este documento.";
    public const string SlugPadrao = "cliente-documento-duplicado";

    public DocumentoClienteJaExisteException()
        : base(MensagemPadrao, SlugPadrao)
    {
    }

    public DocumentoClienteJaExisteException(Exception innerException)
        : base(MensagemPadrao, SlugPadrao, innerException)
    {
    }

    public DocumentoClienteJaExisteException(string mensagem, Exception innerException)
        : base(mensagem, SlugPadrao, innerException)
    {
    }

    public DocumentoClienteJaExisteException(string mensagem)
        : base(mensagem, SlugPadrao)
    {
    }
}
