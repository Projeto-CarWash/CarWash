namespace CarWash.Application.Agenda.Common;

/// <summary>
/// Item da agenda no formato <c>detalhado</c> (RF009): visão operacional
/// completa com cliente, veículo e serviços. PII (CPF/CNPJ/telefone) exposta
/// íntegra — o endpoint sempre envia <c>Cache-Control: no-store</c> (ADR 0004 — L4).
/// </summary>
public sealed class AgendaItemDetalhadoResponse
{
    /// <summary>Identificador do agendamento.</summary>
    public Guid AgendamentoId { get; init; }

    /// <summary>Status no contrato da API (uppercase).</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Filial do agendamento.</summary>
    public Guid FilialId { get; init; }

    /// <summary>Início do agendamento (UTC ISO-8601).</summary>
    public DateTime Inicio { get; init; }

    /// <summary>Fim do agendamento (UTC ISO-8601).</summary>
    public DateTime Fim { get; init; }

    /// <summary>Duração total denormalizada, em minutos.</summary>
    public int DuracaoTotalMin { get; init; }

    /// <summary>Valor total denormalizado.</summary>
    public decimal ValorTotal { get; init; }

    /// <summary>Cliente titular do agendamento.</summary>
    public AgendaClienteResponse Cliente { get; init; } = new();

    /// <summary>Veículo do agendamento.</summary>
    public AgendaVeiculoResponse Veiculo { get; init; } = new();

    /// <summary>Serviços do agendamento, na ordem de criação.</summary>
    public IReadOnlyList<AgendaServicoResponse> Servicos { get; init; } = [];

    /// <summary>Observações livres do agendamento.</summary>
    public string? Observacoes { get; init; }

    /// <summary>Data de criação do agendamento (UTC ISO-8601).</summary>
    public DateTime CriadoEm { get; init; }

    /// <summary>Data da última atualização do agendamento (UTC ISO-8601).</summary>
    public DateTime AtualizadoEm { get; init; }
}

/// <summary>Cliente titular no formato detalhado da agenda.</summary>
public sealed class AgendaClienteResponse
{
    /// <summary>Identificador do cliente.</summary>
    public Guid Id { get; init; }

    /// <summary>Nome do cliente.</summary>
    public string Nome { get; init; } = string.Empty;

    /// <summary>Documento do cliente: CPF (PF) ou CNPJ (PJ).</summary>
    public string? CpfCnpj { get; init; }

    /// <summary>Telefone fixo do cliente (opcional).</summary>
    public string? Telefone { get; init; }

    /// <summary>Celular do cliente.</summary>
    public string Celular { get; init; } = string.Empty;
}

/// <summary>Veículo no formato detalhado da agenda.</summary>
public sealed class AgendaVeiculoResponse
{
    /// <summary>Identificador do veículo.</summary>
    public Guid Id { get; init; }

    /// <summary>Placa do veículo.</summary>
    public string Placa { get; init; } = string.Empty;

    /// <summary>Modelo do veículo.</summary>
    public string Modelo { get; init; } = string.Empty;

    /// <summary>Fabricante do veículo.</summary>
    public string Fabricante { get; init; } = string.Empty;

    /// <summary>Cor do veículo.</summary>
    public string Cor { get; init; } = string.Empty;
}

/// <summary>
/// Serviço no formato detalhado da agenda. Duração e preço são o snapshot
/// aplicado no momento do agendamento (RN006).
/// </summary>
public sealed class AgendaServicoResponse
{
    /// <summary>Identificador do serviço de catálogo.</summary>
    public Guid Id { get; init; }

    /// <summary>Nome do serviço.</summary>
    public string Nome { get; init; } = string.Empty;

    /// <summary>Duração aplicada (snapshot RN006), em minutos.</summary>
    public int DuracaoMin { get; init; }

    /// <summary>Preço aplicado (snapshot RN006).</summary>
    public decimal Preco { get; init; }
}
