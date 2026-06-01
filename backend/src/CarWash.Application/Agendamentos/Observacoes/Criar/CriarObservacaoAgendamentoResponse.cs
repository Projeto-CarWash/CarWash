using CarWash.Application.Agendamentos.Observacoes.Common;

namespace CarWash.Application.Agendamentos.Observacoes.Criar;

public sealed class CriarObservacaoAgendamentoResponse
{
    public string Message { get; set; } = "Observação logística registrada com sucesso.";

    public AgendamentoObservacaoResponse Data { get; set; } = new();

    public string TraceId { get; set; } = string.Empty;
}
