namespace CarWash.Application.Agenda.Common;

/// <summary>
/// Item da agenda no formato <c>simples</c> (RF009). Resumo compacto de um
/// evento — exatamente 8 campos, derivados de <see cref="AgendaProjecao"/>.
/// </summary>
public sealed class AgendaItemSimplesResponse
{
    /// <summary>Identificador do agendamento.</summary>
    public Guid AgendamentoId { get; init; }

    /// <summary>Início do agendamento (UTC ISO-8601).</summary>
    public DateTime Inicio { get; init; }

    /// <summary>Fim do agendamento (UTC ISO-8601).</summary>
    public DateTime Fim { get; init; }

    /// <summary>
    /// Título derivado: nome do primeiro serviço do agendamento
    /// (L2 da ADR 0004). <c>"Agendamento"</c> quando não há serviços.
    /// </summary>
    public string Titulo { get; init; } = string.Empty;

    /// <summary>Status no contrato da API (uppercase).</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Nome do cliente titular.</summary>
    public string ClienteNome { get; init; } = string.Empty;

    /// <summary>Placa do veículo.</summary>
    public string VeiculoPlaca { get; init; } = string.Empty;

    /// <summary>
    /// Resumo curto dos serviços (L3 da ADR 0004): <c>"&lt;nome&gt;"</c> para 1,
    /// <c>"&lt;nome&gt; + &lt;N-1&gt;"</c> para N&gt;1, <c>"Sem serviços"</c> para 0.
    /// </summary>
    public string ServicosResumo { get; init; } = string.Empty;
}
