using CarWash.Domain.Entities;

namespace CarWash.Application.Interfaces;

public interface IClienteRepository
{
    Task<bool> ExisteCpfAsync(string cpf, CancellationToken cancellationToken);

    Task<bool> ExisteCnpjAsync(string cnpj, CancellationToken cancellationToken);

    /// <summary>
    /// Verifica se o e-mail já está em uso por outro cliente. Quando
    /// <paramref name="ignoreClienteId"/> é informado, ignora o próprio cliente
    /// (usado no PUT para permitir manter o mesmo e-mail).
    /// </summary>
    Task<bool> ExisteEmailAsync(string email, Guid? ignoreClienteId, CancellationToken cancellationToken);

    Task<Cliente?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    Task AdicionarAsync(Cliente cliente, string correlationId, Guid? usuarioId, CancellationToken cancellationToken);

    /// <summary>Persiste alterações pendentes (Update).</summary>
    Task SalvarAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Lista clientes paginados com filtro opcional por nome / documento (cpf/cnpj
    /// dígitos) / email / cidade. Ordenação por <c>nome ASC</c>.
    /// </summary>
    Task<(IReadOnlyList<Cliente> Itens, int Total)> ListarAsync(
        string? busca,
        bool? ativo,
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken);
}
