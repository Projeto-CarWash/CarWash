using CarWash.Domain.Entities;
using CarWash.Domain.Enums;

namespace CarWash.Application.Agendamentos.ObterPorId;

public class ObterAgendamentoPorIdResponse
{
    public string Message { get; set; } = "Agendamento encontrado.";

    public ObterAgendamentoPorIdData Data { get; set; } = new();

    public string TraceId { get; set; } = string.Empty;
}

public class ObterAgendamentoPorIdData
{
    public Guid Id { get; set; }

    public Guid FilialId { get; set; }

    public Guid ClienteId { get; set; }

    public Guid VeiculoId { get; set; }

    public Guid? ResponsavelId { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime Inicio { get; set; }

    public DateTime Fim { get; set; }

    public int DuracaoTotalMin { get; set; }

    public decimal ValorTotal { get; set; }

    public string? Observacoes { get; set; }

    public int Versao { get; set; }

    public DateTime CriadoEm { get; set; }

    public DateTime AtualizadoEm { get; set; }

    public static ObterAgendamentoPorIdData FromEntity(Agendamento agendamento)
    {
        ArgumentNullException.ThrowIfNull(agendamento);

        return new ObterAgendamentoPorIdData
        {
            Id = agendamento.Id,
            FilialId = agendamento.FilialId,
            ClienteId = agendamento.ClienteId,
            VeiculoId = agendamento.VeiculoId,
            ResponsavelId = agendamento.ResponsavelId,
            Status = agendamento.Status.ToDbValue().ToUpperInvariant(),
            Inicio = agendamento.Inicio,
            Fim = agendamento.Fim,
            DuracaoTotalMin = agendamento.DuracaoTotalMin,
            ValorTotal = agendamento.ValorTotal,
            Observacoes = agendamento.Observacoes,
            Versao = agendamento.Versao,
            CriadoEm = agendamento.CriadoEm,
            AtualizadoEm = agendamento.AtualizadoEm,
        };
    }
}
