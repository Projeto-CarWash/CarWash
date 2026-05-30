using CarWash.Application.Common.Exceptions;

namespace CarWash.Application.Agendamentos.Common;

/// <summary>
/// Indica que a mesma <c>idempotencyKey</c> já foi usada para uma requisição com
/// payload diferente (RF015): o cliente reaproveitou indevidamente uma chave de
/// idempotência. Replay legítimo (mesma key + mesmo payload) NÃO chega aqui — é
/// resolvido com o retorno da resposta original. Herda de
/// <see cref="ConflictException"/> para o mapeamento 409 + slug no middleware.
/// </summary>
#pragma warning disable RCS1194 // Exceção semântica; o construtor padrão cobre o uso real.
public sealed class IdempotenciaConflitanteException : ConflictException
#pragma warning restore RCS1194
{
    public const string MensagemPadrao =
        "A chave de idempotência já foi usada para uma requisição diferente.";

    public const string SlugPadrao = "idempotencia-conflito";

    public IdempotenciaConflitanteException()
        : base(MensagemPadrao, SlugPadrao)
    {
    }

    public IdempotenciaConflitanteException(string mensagem)
        : base(mensagem, SlugPadrao)
    {
    }

    public IdempotenciaConflitanteException(string mensagem, Exception innerException)
        : base(mensagem, SlugPadrao, innerException)
    {
    }
}
