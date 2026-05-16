using CarWash.Domain.Entities;

namespace CarWash.Application.Interfaces;

public interface IClienteRepository
{
    Task<bool> ExisteCpfAsync(string cpf, CancellationToken cancellationToken);

    Task<bool> ExisteCnpjAsync(string cnpj, CancellationToken cancellationToken);

    Task<Cliente?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    Task AdicionarAsync(Cliente cliente, string correlationId, Guid? usuarioId, CancellationToken cancellationToken);
}
