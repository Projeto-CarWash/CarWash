namespace CarWash.Application.Agenda.Common;

/// <summary>
/// Linha crua projetada do EF Core para um evento da agenda (RF009). É a fonte
/// <b>única</b> de dados — tanto o formato simples quanto o detalhado derivam
/// desta projeção, garantindo que campos compartilhados
/// (<see cref="AgendamentoId"/>, <see cref="Inicio"/>, <see cref="Status"/>,
/// cliente e placa) sejam idênticos entre os dois formatos.
/// </summary>
public sealed record AgendaProjecao
{
    /// <summary>Identificador do agendamento.</summary>
    public Guid AgendamentoId { get; init; }

    /// <summary>Status persistido no banco (<c>agendado</c>/<c>cancelado</c>/<c>finalizado</c>).</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Filial do agendamento.</summary>
    public Guid FilialId { get; init; }

    /// <summary>Início do agendamento (UTC).</summary>
    public DateTime Inicio { get; init; }

    /// <summary>Fim do agendamento (UTC).</summary>
    public DateTime Fim { get; init; }

    /// <summary>Duração total denormalizada, em minutos.</summary>
    public int DuracaoTotalMin { get; init; }

    /// <summary>Valor total denormalizado.</summary>
    public decimal ValorTotal { get; init; }

    /// <summary>Observações livres do agendamento.</summary>
    public string? Observacoes { get; init; }

    /// <summary>Data de criação do agendamento (UTC).</summary>
    public DateTime CriadoEm { get; init; }

    /// <summary>Data da última atualização do agendamento (UTC).</summary>
    public DateTime AtualizadoEm { get; init; }

    /// <summary>Identificador do cliente titular.</summary>
    public Guid ClienteId { get; init; }

    /// <summary>Nome do cliente titular.</summary>
    public string ClienteNome { get; init; } = string.Empty;

    /// <summary>CPF do cliente, quando pessoa física.</summary>
    public string? ClienteCpf { get; init; }

    /// <summary>CNPJ do cliente, quando pessoa jurídica.</summary>
    public string? ClienteCnpj { get; init; }

    /// <summary>Telefone fixo do cliente (opcional).</summary>
    public string? ClienteTelefone { get; init; }

    /// <summary>Celular do cliente (obrigatório no cadastro).</summary>
    public string ClienteCelular { get; init; } = string.Empty;

    /// <summary>Identificador do veículo.</summary>
    public Guid VeiculoId { get; init; }

    /// <summary>Placa do veículo.</summary>
    public string VeiculoPlaca { get; init; } = string.Empty;

    /// <summary>Modelo do veículo.</summary>
    public string VeiculoModelo { get; init; } = string.Empty;

    /// <summary>Fabricante do veículo.</summary>
    public string VeiculoFabricante { get; init; } = string.Empty;

    /// <summary>Cor do veículo.</summary>
    public string VeiculoCor { get; init; } = string.Empty;

    /// <summary>
    /// Serviços do agendamento, já ordenados por <c>CriadoEm ASC, Id ASC</c>
    /// (L2/L3 da ADR 0004). Duração e preço são o snapshot aplicado (RN006).
    /// </summary>
    public IReadOnlyList<AgendaServicoProjecao> Servicos { get; init; } = [];
}

/// <summary>
/// Serviço de um agendamento conforme projetado do EF. Duração e preço vêm do
/// snapshot <see cref="AgendaServicoProjecao.DuracaoMin"/> /
/// <see cref="AgendaServicoProjecao.Preco"/> aplicado no momento do agendamento
/// (RN006), não do catálogo atual de <c>Servico</c>.
/// </summary>
public sealed record AgendaServicoProjecao
{
    /// <summary>Identificador do item de agendamento (usado só na ordenação).</summary>
    public Guid ItemId { get; init; }

    /// <summary>Identificador do serviço de catálogo.</summary>
    public Guid Id { get; init; }

    /// <summary>Nome do serviço de catálogo.</summary>
    public string Nome { get; init; } = string.Empty;

    /// <summary>Duração aplicada (snapshot RN006), em minutos.</summary>
    public int DuracaoMin { get; init; }

    /// <summary>Preço aplicado (snapshot RN006).</summary>
    public decimal Preco { get; init; }
}
