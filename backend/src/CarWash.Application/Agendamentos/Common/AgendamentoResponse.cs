namespace CarWash.Application.Agendamentos.Common;

/// <summary>
/// Representação HTTP de um agendamento criado (RF007/RF019/RF020/RF024).
/// Inclui titular do veículo e responsável selecionado (RF024/CA009).
/// </summary>
public sealed class AgendamentoResponse
{
    public Guid Id { get; init; }

    public Guid FilialId { get; init; }

    public Guid ClienteId { get; init; }

    public Guid VeiculoId { get; init; }

    public Guid ResponsavelId { get; init; }

    public ResponsavelDto Responsavel { get; init; } = new();

    public string Status { get; init; } = string.Empty;

    public DateTime Inicio { get; init; }

    public DateTime Fim { get; init; }

    public int DuracaoTotalMin { get; init; }

    public decimal ValorTotal { get; init; }

    public string? Observacoes { get; init; }

    public int Versao { get; init; }

    public IReadOnlyList<AgendamentoServicoResponse> Itens { get; init; } =
        Array.Empty<AgendamentoServicoResponse>();

    public DateTime CriadoEm { get; init; }

    public string Mensagem { get; init; } = string.Empty;

    public string TraceId { get; init; } = string.Empty;
}

/// <summary>Dados do responsável no payload de resposta (RF024/CA009).</summary>
public sealed class ResponsavelDto
{
    public Guid Id { get; init; }

    public string Nome { get; init; } = string.Empty;

    public string Documento { get; init; } = string.Empty;

    public string GrauVinculo { get; init; } = string.Empty;
}
