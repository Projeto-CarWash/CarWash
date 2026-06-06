namespace CarWash.Application.Clientes.HistoricoAtendimentos.Common;

public sealed class HistoricoAtendimentoResponse
{
    public Guid AgendamentoId { get; set; }

    public DateOnly Data { get; set; }

    public DateTimeOffset HoraInicio { get; set; }

    public DateTimeOffset HoraFim { get; set; }

    public string Status { get; set; } = string.Empty;

    public HistoricoFilialResponse Filial { get; set; } = new();

    public HistoricoVeiculoResponse? Veiculo { get; set; }

    public List<HistoricoServicoResponse> Servicos { get; set; } = [];

    public int DuracaoTotalMin { get; set; }

    public decimal ValorTotal { get; set; }

    public HistoricoUsuarioResponsavelResponse? UsuarioResponsavel { get; set; }

    public string? ObservacoesLogisticas { get; set; }

    public string? MotivoCancelamento { get; set; }

    public DateTimeOffset? ConcluidoEm { get; set; }

    public DateTimeOffset? CanceladoEm { get; set; }

    public string? Origem { get; set; }
}

public sealed class HistoricoFilialResponse
{
    public Guid Id { get; set; }

    public string Nome { get; set; } = string.Empty;
}

public sealed class HistoricoVeiculoResponse
{
    public string Placa { get; set; } = string.Empty;

    public string? Modelo { get; set; }
}

public sealed class HistoricoServicoResponse
{
    public Guid Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public int DuracaoMin { get; set; }

    public decimal Preco { get; set; }
}

public sealed class HistoricoUsuarioResponsavelResponse
{
    public Guid Id { get; set; }

    public string Nome { get; set; } = string.Empty;
}
