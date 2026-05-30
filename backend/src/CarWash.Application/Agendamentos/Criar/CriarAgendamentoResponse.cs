namespace CarWash.Application.Agendamentos.Criar;

public class CriarAgendamentoResponse
{
    public string Message { get; set; } = "Agendamento criado com sucesso.";

    public CriarAgendamentoData Data { get; set; } = new();

    public string TraceId { get; set; } = string.Empty;
}

public class CriarAgendamentoData
{
    public Guid Id { get; set; }

    public Guid FilialId { get; set; }

    public Guid ClienteId { get; set; }

    public Guid VeiculoId { get; set; }

    public string Status { get; set; } = "AGENDADO";

    public DateTime Inicio { get; set; }

    public DateTime Fim { get; set; }

    public int DuracaoTotalMin { get; set; }

    public decimal ValorTotal { get; set; }
}
