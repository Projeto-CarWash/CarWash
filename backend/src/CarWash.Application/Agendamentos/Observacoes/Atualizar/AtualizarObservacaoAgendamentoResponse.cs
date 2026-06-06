using CarWash.Application.Agendamentos.Observacoes.Common;

namespace CarWash.Application.Agendamentos.Observacoes.Atualizar;

public sealed class AtualizarObservacaoAgendamentoResponse
{
    public string Message { get; set; } = "Observação logística atualizada com sucesso.";

    public AgendamentoObservacaoResponse Data { get; set; } = new();

    public string TraceId { get; set; } = string.Empty;
}
