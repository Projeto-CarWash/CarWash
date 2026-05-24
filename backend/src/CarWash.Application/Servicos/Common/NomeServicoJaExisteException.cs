using CarWash.Application.Common.Exceptions;

namespace CarWash.Application.Servicos.Common;

/// <summary>
/// Indica que o nome informado já está em uso por outro serviço
/// (índice único <c>uk_servicos_nome</c>). Herda de
/// <see cref="ConflictException"/> para reaproveitar o status 409 + slug no
/// middleware global.
/// </summary>
#pragma warning disable RCS1194 // Exceção semântica; construtores cobrem os usos reais.
public sealed class NomeServicoJaExisteException : ConflictException
#pragma warning restore RCS1194
{
    public const string MensagemPadrao = "Já existe serviço cadastrado com este nome.";
    public const string SlugPadrao = "servico-nome-duplicado";

    public NomeServicoJaExisteException()
        : base(MensagemPadrao, SlugPadrao)
    {
    }

    public NomeServicoJaExisteException(Exception innerException)
        : base(MensagemPadrao, SlugPadrao, innerException)
    {
    }

    public NomeServicoJaExisteException(string mensagem, Exception innerException)
        : base(mensagem, SlugPadrao, innerException)
    {
    }

    public NomeServicoJaExisteException(string mensagem)
        : base(mensagem, SlugPadrao)
    {
    }
}
