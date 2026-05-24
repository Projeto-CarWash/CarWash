using CarWash.Domain.Entities;

namespace CarWash.Application.Interfaces;

public interface IClienteRepository
{
    Task<bool> ExisteCpfAsync(string cpf, CancellationToken cancellationToken);

    Task<bool> ExisteCnpjAsync(string cnpj, CancellationToken cancellationToken);

    Task<Cliente?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExisteAlgumaPlacaAsync(IReadOnlyCollection<string> placas, CancellationToken cancellationToken);

    Task AdicionarAsync(Cliente cliente, IReadOnlyCollection<Veiculo> veiculos, string correlationId, Guid? usuarioId,  CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Veiculo>> ObterVeiculosPorClienteIdAsync(Guid clienteId, CancellationToken cancellationToken);
}
