namespace CarWash.Application.Agendamentos.ObterPorId;

/// <summary>
/// Resposta da consulta detalhada de agendamento (RF010). Contrato HTTP 200:
/// <c>{ message, data: { ...campos do agendamento... }, traceId }</c>.
/// </summary>
public sealed class AgendamentoDetalhadoResponse
{
    public string Message { get; init; } = string.Empty;

    public AgendamentoDetalhadoData Data { get; init; } = new();

    public string TraceId { get; init; } = string.Empty;
}

/// <summary>
/// Dados completos do agendamento na consulta detalhada (RF010).
/// </summary>
public sealed class AgendamentoDetalhadoData
{
    public Guid Id { get; init; }

    public Guid FilialId { get; init; }

    public Guid ClienteId { get; init; }

    public Guid VeiculoId { get; init; }

    public Guid? ResponsavelId { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTime Inicio { get; init; }

    public DateTime Fim { get; init; }

    public int DuracaoTotalMin { get; init; }

    public decimal ValorTotal { get; init; }

    public string? Observacoes { get; init; }

    public int Versao { get; init; }

    public DateTime CriadoEm { get; init; }

    public DateTime AtualizadoEm { get; init; }

    public DateTime? CanceladoEm { get; init; }

    public Guid? CanceladoPor { get; init; }

    public string? MotivoCancelamento { get; init; }

    public Guid CriadoPor { get; init; }

    public IReadOnlyList<Application.Agendamentos.Common.AgendamentoServicoResponse> Itens { get; init; } =
        Array.Empty<Application.Agendamentos.Common.AgendamentoServicoResponse>();
}
