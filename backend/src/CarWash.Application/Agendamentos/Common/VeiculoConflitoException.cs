using CarWash.Application.Common.Exceptions;

namespace CarWash.Application.Agendamentos.Common;

public sealed class VeiculoConflitoException : ConflictException
{
    public const string MensagemPadrao = "Este veículo já possui agendamento ativo nesta janela de horário.";
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
