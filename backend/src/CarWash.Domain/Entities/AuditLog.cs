namespace CarWash.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; private set; }

    public string Evento { get; private set; } = string.Empty;

    public string Entidade { get; private set; } = string.Empty;

    public Guid? EntidadeId { get; private set; }

    public Guid? UsuarioId { get; private set; }

    public string CorrelationId { get; private set; } = string.Empty;

    public string? Dados { get; private set; }

    public DateTimeOffset CriadoEm { get; private set; }

    public AuditLog(
        string evento,
        string entidade,
        Guid? entidadeId,
        Guid? usuarioId,
        string correlationId,
        string? dados)
    {
        Id = Guid.NewGuid();
        Evento = evento;
        Entidade = entidade;
        EntidadeId = entidadeId;
        UsuarioId = usuarioId;
        CorrelationId = correlationId;
        Dados = dados;
        CriadoEm = DateTimeOffset.UtcNow;
    }

    protected AuditLog()
    {
    }
}
