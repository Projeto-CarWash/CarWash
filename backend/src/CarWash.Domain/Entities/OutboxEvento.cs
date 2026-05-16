using CarWash.Domain.Common;
using CarWash.Domain.Enums;

namespace CarWash.Domain.Entities;

/// <summary>
/// Outbox transacional para processamento assíncrono confiável. Schema criado,
/// sem consumidor no MVP.
/// </summary>
public sealed class OutboxEvento
{
    private OutboxEvento()
    {
        Evento = null!;
        Agregado = null!;
        Payload = null!;
        IdempotencyKey = null!;
        StatusRaw = null!;
    }

    public Guid Id { get; private set; }

    public string Evento { get; private set; }

    public string Agregado { get; private set; }

    public Guid AgregadoId { get; private set; }

    public string Payload { get; private set; }

    public string IdempotencyKey { get; private set; }

    public string StatusRaw { get; private set; }

    public StatusOutbox Status => StatusOutboxExtensions.FromDbValue(StatusRaw);

    public int Tentativas { get; private set; }

    public DateTime DisponivelEm { get; private set; }

    public DateTime CriadoEm { get; private set; }

    public DateTime? ProcessadoEm { get; private set; }

    public static OutboxEvento Criar(
        Guid id,
        string evento,
        string agregado,
        Guid agregadoId,
        string payload,
        string idempotencyKey,
        DateTime? disponivelEm = null)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Id do outbox não pode ser vazio.");
        }

        if (string.IsNullOrWhiteSpace(evento) || evento.Length > 80)
        {
            throw new DomainException("Evento do outbox é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(agregado) || agregado.Length > 80)
        {
            throw new DomainException("Agregado do outbox é obrigatório.");
        }

        if (agregadoId == Guid.Empty)
        {
            throw new DomainException("AgregadoId é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new DomainException("Payload é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 120)
        {
            throw new DomainException("Idempotency key é obrigatória.");
        }

        var agora = DateTime.UtcNow;
        return new OutboxEvento
        {
            Id = id,
            Evento = evento,
            Agregado = agregado,
            AgregadoId = agregadoId,
            Payload = payload,
            IdempotencyKey = idempotencyKey,
            StatusRaw = StatusOutbox.Pendente.ToDbValue(),
            Tentativas = 0,
            DisponivelEm = disponivelEm ?? agora,
            CriadoEm = agora,
        };
    }
}
