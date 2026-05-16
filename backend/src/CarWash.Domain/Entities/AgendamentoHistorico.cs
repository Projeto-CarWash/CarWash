using CarWash.Domain.Common;
using CarWash.Domain.Enums;

namespace CarWash.Domain.Entities;

/// <summary>
/// Trilha funcional do agendamento (RN007). Eventos restritos por
/// <c>ck_hist_evento</c>. Sem updates — append-only por regra de aplicação.
/// </summary>
public sealed class AgendamentoHistorico
{
    private AgendamentoHistorico()
    {
        EventoRaw = null!;
    }

    public Guid Id { get; private set; }

    public Guid AgendamentoId { get; private set; }

    public string EventoRaw { get; private set; }

    public EventoHistorico Evento => EventoHistoricoExtensions.FromDbValue(EventoRaw);

    public string? Payload { get; private set; }

    public Guid UsuarioId { get; private set; }

    public DateTime OcorridoEm { get; private set; }

    public static AgendamentoHistorico Registrar(
        Guid id,
        Guid agendamentoId,
        EventoHistorico evento,
        Guid usuarioId,
        string? payload = null)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Id do histórico não pode ser vazio.");
        }

        if (agendamentoId == Guid.Empty)
        {
            throw new DomainException("Histórico exige agendamento.");
        }

        if (usuarioId == Guid.Empty)
        {
            throw new DomainException("Histórico exige usuário autor.");
        }

        return new AgendamentoHistorico
        {
            Id = id,
            AgendamentoId = agendamentoId,
            EventoRaw = evento.ToDbValue(),
            UsuarioId = usuarioId,
            Payload = payload,
            OcorridoEm = DateTime.UtcNow,
        };
    }
}
