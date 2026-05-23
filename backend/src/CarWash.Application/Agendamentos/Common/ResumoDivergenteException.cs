using CarWash.Application.Common.Exceptions;

namespace CarWash.Application.Agendamentos.Common;

/// <summary>
/// Indica que os dados do agendamento mudaram entre a pré-confirmação e a
/// confirmação (RF015): o <c>hashResumo</c> recalculado na confirmação difere do
/// hash assinado no token. Pode ser dados editados pelo usuário ou alteração no
/// catálogo (preço/duração de serviço). Herda de <see cref="ConflictException"/>
/// para reaproveitar o mapeamento 409 + slug no middleware global.
/// </summary>
#pragma warning disable RCS1194 // Exceção semântica; o construtor padrão cobre o uso real.
public sealed class ResumoDivergenteException : ConflictException
#pragma warning restore RCS1194
{
    public const string MensagemPadrao =
        "Os dados do agendamento foram alterados. Revise antes de confirmar.";

    public const string SlugPadrao = "agendamento-resumo-divergente";

    public ResumoDivergenteException()
        : base(MensagemPadrao, SlugPadrao)
    {
    }

    public ResumoDivergenteException(string mensagem)
        : base(mensagem, SlugPadrao)
    {
    }

    public ResumoDivergenteException(string mensagem, Exception innerException)
        : base(mensagem, SlugPadrao, innerException)
    {
    }
}
