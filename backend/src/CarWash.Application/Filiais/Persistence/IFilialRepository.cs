using CarWash.Application.Filiais.Common;
using CarWash.Domain.Entities;

namespace CarWash.Application.Filiais.Persistence;

/// <summary>
/// Porta de persistência da aggregate <see cref="Filial"/>. A implementação concreta
/// vive em <c>CarWash.Infrastructure</c>. Mantém a Application desacoplada do EF Core.
/// Espelha o contrato de <see cref="Usuarios.Persistence.IUsuarioRepository"/>.
/// </summary>
public interface IFilialRepository
{
    /// <summary>Recupera por id (AsNoTracking) ou retorna <c>null</c>.</summary>
    Task<Filial?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Recupera por id com tracking habilitado — uso obrigatório quando a Use Case
    /// vai mutar o agregado antes de <see cref="SalvarAsync"/>.
    /// </summary>
    Task<Filial?> ObterPorIdRastreadoAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Verifica colisão de nome (UK <c>uk_filiais_nome</c>). Case-insensitive (ILIKE)
    /// para alinhar ao comportamento esperado pelo cliente.
    /// </summary>
    Task<bool> ExisteComNomeAsync(string nome, CancellationToken cancellationToken);

    /// <summary>Adiciona o aggregate à unidade de trabalho — não persiste.</summary>
    Task AdicionarAsync(Filial filial, CancellationToken cancellationToken);

    /// <summary>
    /// Persiste mudanças. Traduz <c>DbUpdateException</c> que viole
    /// <c>uk_filiais_nome</c> em <see cref="NomeFilialJaExisteException"/> (409).
    /// </summary>
    Task SalvarAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Projeção mínima usada por outras camadas (ex.: validação de capacidade do
    /// RF008). Retorna o valor de <c>CelulasAtivas</c> da filial ou <c>null</c> se
    /// não existir. AsNoTracking.
    /// </summary>
    Task<int?> ObterCelulasAtivasAsync(Guid filialId, CancellationToken cancellationToken);
}
