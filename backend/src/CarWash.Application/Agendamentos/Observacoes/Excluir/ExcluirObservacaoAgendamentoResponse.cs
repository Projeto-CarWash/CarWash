namespace CarWash.Application.Agendamentos.Observacoes.Excluir;

public sealed class ExcluirObservacaoAgendamentoResponse
{
    public string Message { get; set; } = "Observação logística removida com sucesso.";

    public string TraceId { get; set; } = string.Empty;
}
