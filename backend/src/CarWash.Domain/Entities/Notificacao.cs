using CarWash.Domain.Common;

namespace CarWash.Domain.Entities;

/// <summary>
/// Rastreio de envio de notificações ligadas a agendamentos. Schema criado no MVP
/// sem consumidor (decisão P09).
/// </summary>
public sealed class Notificacao
{
    private Notificacao()
    {
        Tipo = null!;
        Canal = null!;
        Destino = null!;
        IdempotencyKey = null!;
        Status = null!;
    }

    public Guid Id { get; private set; }

    public Guid AgendamentoId { get; private set; }

    public string Tipo { get; private set; }

    public string Canal { get; private set; }

    public string Destino { get; private set; }

    public string IdempotencyKey { get; private set; }

    public string Status { get; private set; }

    public int Tentativas { get; private set; }

    public DateTime? UltimaTentativa { get; private set; }

    public DateTime CriadoEm { get; private set; }

    public static Notificacao Criar(
        Guid id,
        Guid agendamentoId,
        string tipo,
        string canal,
        string destino,
        string idempotencyKey)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Id da notificação não pode ser vazio.");
        }

        if (agendamentoId == Guid.Empty)
        {
            throw new DomainException("Notificação exige agendamento.");
        }

        if (string.IsNullOrWhiteSpace(tipo) || tipo.Length > 30)
        {
            throw new DomainException("Tipo de notificação é obrigatório.");
        }

        if (canal is not ("email" or "whatsapp" or "sms"))
        {
            throw new DomainException("Canal deve ser email, whatsapp ou sms.");
        }

        if (string.IsNullOrWhiteSpace(destino) || destino.Length > 120)
        {
            throw new DomainException("Destino é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 120)
        {
            throw new DomainException("Idempotency key é obrigatória.");
        }

        return new Notificacao
        {
            Id = id,
            AgendamentoId = agendamentoId,
            Tipo = tipo,
            Canal = canal,
            Destino = destino,
            IdempotencyKey = idempotencyKey,
            Status = "pendente",
            Tentativas = 0,
            CriadoEm = DateTime.UtcNow,
        };
    }
}
