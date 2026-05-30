using CarWash.Application.Common.Exceptions;

namespace CarWash.Application.Agendamentos.Common;

public sealed class VeiculoConflitoException : ConflictException
{
    public const string MensagemPadrao = "Já existe agendamento para este veículo no horário informado.";
    public const string SlugPadrao = "veiculo-conflito";

    public VeiculoConflitoException()
        : base(MensagemPadrao, SlugPadrao)
    {
    }

    public VeiculoConflitoException(Exception innerException)
        : base(MensagemPadrao, SlugPadrao, innerException)
    {
    }
}
