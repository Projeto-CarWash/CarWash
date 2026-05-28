using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarWash.Application.Interfaces;

/// <summary>
/// Interface do contexto de banco de dados da aplicação.
/// </summary>
public interface ICarWashDbContext
{
    /// <summary>
    /// Gets o DbSet de utilizadores.
    /// </summary>
    DbSet<User> Users { get; }

    /// <summary>
    /// Gets o DbSet de clientes.
    /// </summary>
    DbSet<Cliente> Clientes { get; }

    /// <summary>
    /// Gets o DbSet de veiculos.
    /// </summary>
    DbSet<Veiculo> Veiculos { get; }

    /// <summary>
    /// Gets o DbSet de sessões.
    /// </summary>
    DbSet<Session> Sessions { get; }

    /// <summary>
    /// Gets o DbSet de serviços.
    /// </summary>
    DbSet<Servico> Servicos { get; }

    /// <summary>
    /// Salva as alterações no banco de dados.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Número de linhas afetadas no banco.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
