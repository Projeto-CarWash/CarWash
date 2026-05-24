using CarWash.Domain.Common;

namespace CarWash.Domain.Entities;

/// <summary>
/// Trilha técnica de eventos críticos (DAT §9.1). Append-only por convenção;
/// preenchida pelo <c>AuditLogInterceptor</c> com mascaramento de dados sensíveis.
/// </summary>
public sealed class AuditLog
{
    private AuditLog()
    {
        Evento = null!;
        Entidade = null!;
        CorrelationId = null!;
    }

    public Guid Id { get; private set; }

    public string Evento { get; private set; }

    public string Entidade { get; private set; }

    public Guid? EntidadeId { get; private set; }

    public Guid? UsuarioId { get; private set; }

    public string CorrelationId { get; private set; }

    public string? Dados { get; private set; }

    public DateTime CriadoEm { get; private set; }

    public static AuditLog Registrar(
        Guid id,
        string evento,
        string entidade,
        string correlationId,
        Guid? entidadeId = null,
        Guid? usuarioId = null,
        string? dados = null)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Id do audit log não pode ser vazio.");
        }

        if (string.IsNullOrWhiteSpace(evento) || evento.Length > 80)
        {
            throw new DomainException("Evento de auditoria é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(entidade) || entidade.Length > 80)
        {
            throw new DomainException("Entidade de auditoria é obrigatória.");
        }

        if (string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > 64)
        {
            throw new DomainException("Correlation id é obrigatório (DAT §9.1).");
        }

        return new AuditLog
        {
            Id = id,
            Evento = evento,
            Entidade = entidade,
            EntidadeId = entidadeId,
            UsuarioId = usuarioId,
            CorrelationId = correlationId,
            Dados = dados,
            CriadoEm = DateTime.UtcNow,
        };
    }
}
