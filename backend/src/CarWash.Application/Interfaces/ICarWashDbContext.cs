using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarWash.Application.Interfaces;

/// <summary>
/// Interface do contexto de banco de dados da aplicação.
/// </summary>
public interface ICarWashDbContext
{
    /// <summary>
    /// Gets a tabela de utilizadores.
    /// </summary>
    DbSet<User> Users { get; }

    /// <summary>
    /// Gets a tabela de clientes.
    /// </summary>
    DbSet<Cliente> Clientes { get; }

    /// <summary>
    /// Gets a tabela de veiculos.
    /// </summary>
    DbSet<Veiculo> Veiculos { get; }

    /// <summary>
    /// Gets a tabela de sessões.
    /// </summary>
    DbSet<Session> Sessions { get; }

    /// <summary>
    /// Salva as alterações no banco de dados.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Número de linhas afetadas no banco.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
