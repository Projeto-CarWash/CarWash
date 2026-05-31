using CarWash.Application.Abstractions;
using CarWash.Domain.Entities;
using CarWash.Infrastructure.Persistence;
using CarWash.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace CarWash.Infrastructure.Auditing;

/// <summary>
/// Implementação default de <see cref="IAuditLogger"/> — grava direto em
/// <c>audit_logs</c> via <c>CarWashDbContext</c>. Mascara dados sensíveis antes
/// de serializar.
/// </summary>
public sealed class AuditLogger : IAuditLogger
{
    private readonly IDbContextFactory<CarWashDbContext> _dbFactory;
    private readonly ICurrentRequestContext _contexto;

    public AuditLogger(IDbContextFactory<CarWashDbContext> dbFactory, ICurrentRequestContext contexto)
    {
        _dbFactory = dbFactory;
        _contexto = contexto;
    }

    /// <inheritdoc/>
    public async Task LogAsync(
        string evento,
        string entidade,
        Guid? entidadeId = null,
        object? dados = null,
        CancellationToken cancellationToken = default)
    {
        string? json = dados is null ? null : AuditDataMasker.Mask(dados);

        var log = AuditLog.Registrar(
            id: Guid.NewGuid(),
            evento: evento,
            entidade: entidade,
            correlationId: _contexto.CorrelationId,
            entidadeId: entidadeId,
            usuarioId: _contexto.UsuarioId,
            dados: json);

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        db.Set<AuditLog>().Add(log);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
