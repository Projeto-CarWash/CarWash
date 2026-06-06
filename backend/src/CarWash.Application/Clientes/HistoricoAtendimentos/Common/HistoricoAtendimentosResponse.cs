namespace CarWash.Application.Clientes.HistoricoAtendimentos.Common;

public sealed class HistoricoAtendimentosResponse
{
    public string Message { get; set; } = string.Empty;

    public List<HistoricoAtendimentoResponse> Data { get; set; } = [];

    public HistoricoAtendimentosMetaResponse Meta { get; set; } = new();

    public string TraceId { get; set; } = string.Empty;
}
