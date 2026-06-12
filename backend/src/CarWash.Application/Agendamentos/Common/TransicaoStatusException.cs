using CarWash.Application.Common.Exceptions;

namespace CarWash.Application.Agendamentos.Common;

/// <summary>
/// Indica que o agendamento não permite a transição de status solicitada
/// (iniciar/finalizar — RF010/RF013). Herda de <see cref="ConflictException"/>
/// para produzir HTTP 409 com slug estável no middleware global.
/// </summary>
public sealed class TransicaoStatusException : ConflictException
{
    public const string SlugPadrao = "agendamento-transicao-status";

    public TransicaoStatusException(string mensagem)
        : base(mensagem, SlugPadrao)
    {
    }
}
