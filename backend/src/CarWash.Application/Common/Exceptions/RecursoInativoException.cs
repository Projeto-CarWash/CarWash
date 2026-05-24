namespace CarWash.Application.Common.Exceptions;

/// <summary>
/// Indica que a operação referenciou um recurso (filial, veículo, cliente ou
/// serviço) que existe mas está inativo — ex.: agendar em filial desativada ou
/// com serviço fora de catálogo. Mapeada para HTTP 422 Unprocessable Entity no
/// <c>ExceptionHandlingMiddleware</c>: a requisição é sintaticamente válida, mas
/// não pode ser processada por uma regra de estado do negócio.
/// </summary>
#pragma warning disable RCS1194 // Exceção semântica; construtores cobrem os usos reais.
public sealed class RecursoInativoException : Exception
#pragma warning restore RCS1194
{
    public const string SlugPadrao = "recurso-inativo";

    public RecursoInativoException(string mensagem)
        : base(mensagem)
    {
    }

    public RecursoInativoException(string mensagem, Exception innerException)
        : base(mensagem, innerException)
    {
    }

    public RecursoInativoException()
        : base("Recurso referenciado está inativo e não pode ser utilizado.")
    {
    }
}
