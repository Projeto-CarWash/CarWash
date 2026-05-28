using CarWash.Domain.Entities;

namespace CarWash.Application.Filiais.Persistence;

/// <summary>
/// Porta de persistência do agregado <see cref="Filial"/>. A implementação
/// concreta vive em <c>CarWash.Infrastructure</c> e traduz violações de UK
/// em <see cref="Common.FilialCodigoJaExisteException"/>,
/// <see cref="Common.FilialCnpjJaExisteException"/> e
/// <see cref="Common.FilialNomeJaExisteException"/> (defesa contra race
/// condition entre pré-check e insert — ADR-0007 §5.2).
/// </summary>
public interface IFilialRepository
{
    Task<bool> ExisteCodigoAsync(string codigo, CancellationToken cancellationToken);

    Task<bool> ExisteCnpjAsync(string cnpj, CancellationToken cancellationToken);

    /// <summary>
    /// Verifica unicidade de nome ignorando caixa via <c>LOWER(nome) =
    /// LOWER($1)</c> — casa com o índice funcional
    /// <c>uk_filiais_nome_lower</c> e é imune a curingas LIKE (<c>%</c>,
    /// <c>_</c>) no termo informado.
    /// </summary>
    Task<bool> ExisteNomeAsync(string nome, CancellationToken cancellationToken);

    /// <summary>
    /// Recupera a filial por id com tracking habilitado (sem AsNoTracking) —
    /// permite que o use case de RF018 mute o agregado (<c>AjustarCelulas</c>)
    /// e persista via <see cref="SalvarAsync"/>. O GET por id também reusa
    /// este método (leitura simples).
    /// </summary>
    Task<Filial?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Persiste a filial recém-criada. Traduz <c>DbUpdateException</c> por
    /// violação de UK em exceções específicas (ADR-0007 §5.2). A linha em
    /// <c>audit_logs</c> é emitida automaticamente pelo
    /// <c>AuditLogInterceptor</c> a partir do evento definido no handler.
    /// </summary>
    Task AdicionarAsync(Filial filial, CancellationToken cancellationToken);

    /// <summary>
    /// Persiste mudanças pendentes do agregado já rastreado (RF018 —
    /// ajuste de <c>celulas_ativas</c>). Encapsula <c>SaveChangesAsync</c>
    /// para manter a Application desacoplada do EF Core.
    /// </summary>
    Task SalvarAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Lista filiais paginadas com filtro opcional por <c>ativo</c> e por
    /// termo de busca (nome / codigo / cidade — comparação case-insensitive).
    /// Ordenação por <c>nome ASC</c>.
    /// </summary>
    Task<(IReadOnlyList<Filial> Itens, int Total)> ListarAsync(
        string? busca,
        bool? ativo,
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken);
}
