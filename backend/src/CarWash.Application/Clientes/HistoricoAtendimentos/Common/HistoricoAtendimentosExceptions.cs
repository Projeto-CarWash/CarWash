namespace CarWash.Application.Clientes.HistoricoAtendimentos.Common;

public sealed class ClienteHistoricoNaoEncontradoException : Exception
{
    public ClienteHistoricoNaoEncontradoException()
        : base("Cliente não encontrado para consulta de histórico.")
    {
    }
}
