namespace CarWash.Application.Agendamentos.Observacoes.Common;

public sealed class AgendamentoNaoEncontradoException : Exception
{
    public AgendamentoNaoEncontradoException()
        : base("Agendamento não encontrado.")
    {
    }
}

public sealed class ObservacaoAgendamentoNaoEncontradaException : Exception
{
    public ObservacaoAgendamentoNaoEncontradaException()
        : base("Observação logística não encontrada para este agendamento.")
    {
    }
}

public sealed class ObservacaoAgendamentoEstadoInvalidoException : Exception
{
    public ObservacaoAgendamentoEstadoInvalidoException()
        : base("A observação logística não pode ser alterada no estado atual.")
    {
    }
}
