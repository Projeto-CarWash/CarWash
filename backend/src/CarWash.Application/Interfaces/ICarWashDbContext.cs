using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarWash.Application.Interfaces;

/// <summary>
/// Interface do contexto de banco de dados da aplicação.
/// </summary>
public interface ICarWashDbContext
{
    /// <summary>
    /// Gets or sets a tabela de utilizadores.
    /// </summary>
    DbSet<User> Users { get; set; }

    /// <summary>
    /// Gets or sets a tabela de sessões.
    /// </summary>
    DbSet<Session> Sessions { get; set; }

    /// <summary>
    /// Salva as alterações no banco de dados.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Número de linhas afetadas no banco.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
