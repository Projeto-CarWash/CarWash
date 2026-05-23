namespace CarWash.Domain.Common;

/// <summary>
/// Permite ao interceptor da Infrastructure preencher <c>CriadoEm</c> e <c>AtualizadoEm</c>
/// sem expor setters públicos individuais. As entidades implementam explicitamente
/// para manter o domínio limpo.
/// </summary>
public interface IAuditableSetter
{
    void SetCriadoEm(DateTime valor);

    void SetAtualizadoEm(DateTime valor);
}
