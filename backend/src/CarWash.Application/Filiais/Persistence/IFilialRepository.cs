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
    /// Verifica unicidade de nome ignorando caixa (UK funcional
    /// <c>uk_filiais_nome_lower</c>). O termo informado é comparado em
    /// <c>LOWER(nome)</c>.
    /// </summary>
    Task<bool> ExisteNomeAsync(string nome, CancellationToken cancellationToken);

    Task<Filial?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Persiste a filial recém-criada dentro de uma transação curta com a
    /// linha de <c>audit_logs</c>. Traduz <c>DbUpdateException</c> por
    /// violação de UK em exceções específicas (ADR-0007 §5.2).
    /// </summary>
    Task AdicionarAsync(Filial filial, string correlationId, Guid? usuarioId, CancellationToken cancellationToken);

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
