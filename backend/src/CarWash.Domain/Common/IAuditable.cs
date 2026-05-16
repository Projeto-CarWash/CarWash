namespace CarWash.Domain.Common;

/// <summary>
/// Marker para entidades que mantêm <c>CriadoEm</c> / <c>AtualizadoEm</c> (DAT §9 e DB001 §07).
/// O <c>AuditableEntitiesInterceptor</c> popula os campos automaticamente.
/// </summary>
public interface IAuditable
{
    DateTime CriadoEm { get; }

    DateTime AtualizadoEm { get; }
}
