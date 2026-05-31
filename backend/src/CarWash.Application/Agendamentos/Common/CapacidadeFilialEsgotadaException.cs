using CarWash.Application.Common.Exceptions;

namespace CarWash.Application.Agendamentos.Common;

/// <summary>
/// RF008/RF018: a filial atingiu o teto de simultâneos (<c>celulas_ativas</c>,
/// RN009) na janela solicitada. Herda de <see cref="ConflictException"/> →
/// 409 + slug "capacidade-filial-esgotada" via <c>ExceptionHandlingMiddleware</c>
/// (caminho genérico de <see cref="ConflictException"/> já existente — sem
/// novo catch).
/// </summary>
#pragma warning disable RCS1194 // Exceção semântica; os construtores cobrem os usos reais.
public sealed class CapacidadeFilialEsgotadaException : ConflictException
#pragma warning restore RCS1194
{
    public const string MensagemPadrao =
        "Capacidade da filial esgotada para o horário solicitado.";

    public const string SlugPadrao = "capacidade-filial-esgotada";

    public CapacidadeFilialEsgotadaException()
        : base(MensagemPadrao, SlugPadrao)
    {
    }

    public CapacidadeFilialEsgotadaException(Exception innerException)
        : base(MensagemPadrao, SlugPadrao, innerException)
    {
    }
}
