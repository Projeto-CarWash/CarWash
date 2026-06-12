using CarWash.Application.Common.Exceptions;

namespace CarWash.Application.Agendamentos.Common;

public sealed class CapacidadeFilialAtingidaException : ConflictException
{
    public const string MensagemPadrao = "Capacidade máxima da filial atingida para este horário.";
    public const string SlugPadrao = "capacidade-filial";

    public CapacidadeFilialAtingidaException()
        : base(MensagemPadrao, SlugPadrao)
    {
    }

    public CapacidadeFilialAtingidaException(Exception innerException)
        : base(MensagemPadrao, SlugPadrao, innerException)
    {
    }
}
