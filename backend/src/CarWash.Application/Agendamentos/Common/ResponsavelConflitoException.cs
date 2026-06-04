using CarWash.Application.Common.Exceptions;

namespace CarWash.Application.Agendamentos.Common;

public sealed class ResponsavelConflitoException : ConflictException
{
    public const string MensagemPadrao = "Responsável informado não pertence ao cliente do agendamento.";
    public const string SlugPadrao = "responsavel-conflito";

    public ResponsavelConflitoException()
        : base(MensagemPadrao, SlugPadrao)
    {
    }
}
