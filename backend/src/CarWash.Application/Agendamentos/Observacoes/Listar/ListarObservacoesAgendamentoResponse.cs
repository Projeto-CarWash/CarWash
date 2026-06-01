using CarWash.Application.Agendamentos.Observacoes.Common;

namespace CarWash.Application.Agendamentos.Observacoes.Listar;

public sealed class ListarObservacoesAgendamentoResponse
{
    public string Message { get; set; } = "Observações logísticas consultadas com sucesso.";

    public List<AgendamentoObservacaoResponse> Data { get; set; } = [];

    public string TraceId { get; set; } = string.Empty;
}
