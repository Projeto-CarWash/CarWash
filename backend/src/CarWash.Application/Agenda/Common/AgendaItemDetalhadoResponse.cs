namespace CarWash.Application.Agenda.Common;

/// <summary>
/// Item da agenda no formato <c>detalhado</c> (RF009): visão operacional
/// completa com cliente, veículo e serviços. PII (CPF/CNPJ/telefone) exposta
/// íntegra — o endpoint sempre envia <c>Cache-Control: no-store</c> (ADR 0004 — L4).
/// </summary>
public sealed class AgendaItemDetalhadoResponse
{
    /// <summary>Gets identificador do agendamento.</summary>
    public Guid AgendamentoId { get; init; }

    /// <summary>Gets status no contrato da API (uppercase).</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Gets filial do agendamento.</summary>
    public Guid FilialId { get; init; }

    /// <summary>Gets início do agendamento (UTC ISO-8601).</summary>
    public DateTime Inicio { get; init; }

    /// <summary>Gets fim do agendamento (UTC ISO-8601).</summary>
    public DateTime Fim { get; init; }

    /// <summary>Gets duração total denormalizada, em minutos.</summary>
    public int DuracaoTotalMin { get; init; }

    /// <summary>Gets valor total denormalizado.</summary>
    public decimal ValorTotal { get; init; }

    /// <summary>Gets cliente titular do agendamento.</summary>
    public AgendaClienteResponse Cliente { get; init; } = new();

    /// <summary>Gets veículo do agendamento.</summary>
    public AgendaVeiculoResponse Veiculo { get; init; } = new();

    /// <summary>Gets serviços do agendamento, na ordem de criação.</summary>
    public IReadOnlyList<AgendaServicoResponse> Servicos { get; init; } = [];

    /// <summary>Gets observações livres do agendamento.</summary>
    public string? Observacoes { get; init; }

    /// <summary>Gets data de criação do agendamento (UTC ISO-8601).</summary>
    public DateTime CriadoEm { get; init; }

    /// <summary>Gets data da última atualização do agendamento (UTC ISO-8601).</summary>
    public DateTime AtualizadoEm { get; init; }
}

/// <summary>Cliente titular no formato detalhado da agenda.</summary>
public sealed class AgendaClienteResponse
{
    /// <summary>Gets identificador do cliente.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets nome do cliente.</summary>
    public string Nome { get; init; } = string.Empty;

    /// <summary>Gets documento do cliente: CPF (PF) ou CNPJ (PJ).</summary>
    public string? CpfCnpj { get; init; }

    /// <summary>Gets telefone fixo do cliente (opcional).</summary>
    public string? Telefone { get; init; }

    /// <summary>Gets celular do cliente.</summary>
    public string Celular { get; init; } = string.Empty;
}

/// <summary>Veículo no formato detalhado da agenda.</summary>
public sealed class AgendaVeiculoResponse
{
    /// <summary>Gets identificador do veículo.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets placa do veículo.</summary>
    public string Placa { get; init; } = string.Empty;

    /// <summary>Gets modelo do veículo.</summary>
    public string Modelo { get; init; } = string.Empty;

    /// <summary>Gets fabricante do veículo.</summary>
    public string Fabricante { get; init; } = string.Empty;

    /// <summary>Gets cor do veículo.</summary>
    public string Cor { get; init; } = string.Empty;
}

/// <summary>
/// Serviço no formato detalhado da agenda. Duração e preço são o snapshot
/// aplicado no momento do agendamento (RN006).
/// </summary>
public sealed class AgendaServicoResponse
{
    /// <summary>Gets identificador do serviço de catálogo.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets nome do serviço.</summary>
    public string Nome { get; init; } = string.Empty;

    /// <summary>Gets duração aplicada (snapshot RN006), em minutos.</summary>
    public int DuracaoMin { get; init; }

    /// <summary>Gets preço aplicado (snapshot RN006).</summary>
    public decimal Preco { get; init; }
}
