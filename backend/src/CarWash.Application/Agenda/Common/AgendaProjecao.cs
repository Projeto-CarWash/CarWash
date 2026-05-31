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
    /// <summary>Gets identificador do agendamento.</summary>
    public Guid AgendamentoId { get; init; }

    /// <summary>Gets status persistido no banco (<c>agendado</c>/<c>cancelado</c>/<c>finalizado</c>).</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Gets filial do agendamento.</summary>
    public Guid FilialId { get; init; }

    /// <summary>Gets início do agendamento (UTC).</summary>
    public DateTime Inicio { get; init; }

    /// <summary>Gets fim do agendamento (UTC).</summary>
    public DateTime Fim { get; init; }

    /// <summary>Gets duração total denormalizada, em minutos.</summary>
    public int DuracaoTotalMin { get; init; }

    /// <summary>Gets valor total denormalizado.</summary>
    public decimal ValorTotal { get; init; }

    /// <summary>Gets observações livres do agendamento.</summary>
    public string? Observacoes { get; init; }

    /// <summary>Gets data de criação do agendamento (UTC).</summary>
    public DateTime CriadoEm { get; init; }

    /// <summary>Gets data da última atualização do agendamento (UTC).</summary>
    public DateTime AtualizadoEm { get; init; }

    /// <summary>Gets identificador do cliente titular.</summary>
    public Guid ClienteId { get; init; }

    /// <summary>Gets nome do cliente titular.</summary>
    public string ClienteNome { get; init; } = string.Empty;

    /// <summary>Gets cPF do cliente, quando pessoa física.</summary>
    public string? ClienteCpf { get; init; }

    /// <summary>Gets cNPJ do cliente, quando pessoa jurídica.</summary>
    public string? ClienteCnpj { get; init; }

    /// <summary>Gets telefone fixo do cliente (opcional).</summary>
    public string? ClienteTelefone { get; init; }

    /// <summary>Gets celular do cliente (obrigatório no cadastro).</summary>
    public string ClienteCelular { get; init; } = string.Empty;

    /// <summary>Gets identificador do veículo.</summary>
    public Guid VeiculoId { get; init; }

    /// <summary>Gets placa do veículo.</summary>
    public string VeiculoPlaca { get; init; } = string.Empty;

    /// <summary>Gets modelo do veículo.</summary>
    public string VeiculoModelo { get; init; } = string.Empty;

    /// <summary>Gets fabricante do veículo.</summary>
    public string VeiculoFabricante { get; init; } = string.Empty;

    /// <summary>Gets cor do veículo.</summary>
    public string VeiculoCor { get; init; } = string.Empty;

    /// <summary>
    /// Gets serviços do agendamento, já ordenados por <c>CriadoEm ASC, Id ASC</c>
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
    /// <summary>Gets identificador do item de agendamento (usado só na ordenação).</summary>
    public Guid ItemId { get; init; }

    /// <summary>Gets identificador do serviço de catálogo.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets nome do serviço de catálogo.</summary>
    public string Nome { get; init; } = string.Empty;

    /// <summary>Gets duração aplicada (snapshot RN006), em minutos.</summary>
    public int DuracaoMin { get; init; }

    /// <summary>Gets preço aplicado (snapshot RN006).</summary>
    public decimal Preco { get; init; }
}
