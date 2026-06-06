namespace CarWash.Application.Dashboard.Metricas.Common;

public sealed class DashboardMetricasResponse
{
    public string Message { get; set; } = string.Empty;

    public DashboardMetricasDataResponse Data { get; set; } = new();

    public string TraceId { get; set; } = string.Empty;
}

public sealed class DashboardMetricasDataResponse
{
    public DashboardPeriodoResponse Periodo { get; set; } = new();

    public DashboardFiltrosAplicadosResponse FiltrosAplicados { get; set; } = new();

    public DashboardOperacionalResponse Operacional { get; set; } = new();

    public DashboardFinanceiroResponse Financeiro { get; set; } = new();
}

public sealed class DashboardPeriodoResponse
{
    public DateTimeOffset DataInicio { get; set; }

    public DateTimeOffset DataFim { get; set; }
}

public sealed class DashboardFiltrosAplicadosResponse
{
    public Guid? FilialId { get; set; }

    public Guid? ClienteId { get; set; }

    public string? Status { get; set; }
}

public sealed class DashboardOperacionalResponse
{
    public int TotalAtendimentos { get; set; }

    public int Pendentes { get; set; }

    public int Concluidos { get; set; }

    public int Cancelados { get; set; }

    public decimal TaxaConclusao { get; set; }

    public int TempoMedioAtendimentoMin { get; set; }
}

public sealed class DashboardFinanceiroResponse
{
    public decimal FaturamentoTotal { get; set; }

    public decimal TicketMedio { get; set; }

    public List<DashboardFaturamentoFilialResponse> FaturamentoPorFilial { get; set; } = [];

    public List<DashboardFaturamentoServicoResponse> FaturamentoPorServico { get; set; } = [];

    public decimal ValorMedioPorCliente { get; set; }
}

public sealed class DashboardFaturamentoFilialResponse
{
    public Guid FilialId { get; set; }

    public string Nome { get; set; } = string.Empty;

    public decimal Valor { get; set; }
}

public sealed class DashboardFaturamentoServicoResponse
{
    public Guid ServicoId { get; set; }

    public string Nome { get; set; } = string.Empty;

    public decimal Valor { get; set; }
}
