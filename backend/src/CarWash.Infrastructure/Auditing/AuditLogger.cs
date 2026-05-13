using CarWash.Application.Abstractions;
using CarWash.Domain.Entities;
using CarWash.Infrastructure.Persistence;
using CarWash.Infrastructure.Security;

namespace CarWash.Infrastructure.Auditing;

/// <summary>
/// Implementação default de <see cref="IAuditLogger"/> — grava direto em
/// <c>audit_logs</c> via <c>CarWashDbContext</c>. Mascara dados sensíveis antes
/// de serializar.
/// </summary>
public sealed class AuditLogger : IAuditLogger
{
    private readonly CarWashDbContext _db;
    private readonly ICurrentRequestContext _contexto;

    public AuditLogger(CarWashDbContext db, ICurrentRequestContext contexto)
    {
        _db = db;
        _contexto = contexto;
    }

    public async Task LogAsync(
        string evento,
        string entidade,
        Guid? entidadeId = null,
        object? dados = null,
        CancellationToken cancellationToken = default)
    {
        var json = dados is null ? null : AuditDataMasker.Mask(dados);

        var log = AuditLog.Registrar(
            id: Guid.NewGuid(),
            evento: evento,
            entidade: entidade,
            correlationId: _contexto.CorrelationId,
            entidadeId: entidadeId,
            usuarioId: _contexto.UsuarioId,
            dados: json);

        _db.Set<AuditLog>().Add(log);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
