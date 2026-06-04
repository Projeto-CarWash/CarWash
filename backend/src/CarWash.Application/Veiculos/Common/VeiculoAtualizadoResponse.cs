namespace CarWash.Application.Veiculos.Common;

/// <summary>
/// DTO de resposta de atualização de veículo (PUT/PATCH). Envelope padronizado
/// com <c>message</c>, <c>data</c> e <c>traceId</c> — segue o padrão da Agenda.
/// </summary>
public sealed class VeiculoAtualizadoResponse
{
    public string Message { get; init; } = "Veículo atualizado com sucesso.";

    public VeiculoAtualizadoData Data { get; init; } = new();

    public string TraceId { get; init; } = string.Empty;
}

public sealed class VeiculoAtualizadoData
{
    public Guid Id { get; init; }

    public Guid ClienteId { get; init; }

    public string Placa { get; init; } = string.Empty;

    public string Modelo { get; init; } = string.Empty;

    public string Fabricante { get; init; } = string.Empty;

    public string Cor { get; init; } = string.Empty;

    public int? Ano { get; init; }

    public bool Ativo { get; init; }

    public DateTime AtualizadoEm { get; init; }
}
