using CarWash.Domain.Entities;

namespace CarWash.Application.Responsaveis.Persistence;

public interface IResponsavelRepository
{
    Task<bool> ExisteDocumentoAsync(string documento, CancellationToken cancellationToken);

    Task AdicionarAsync(Responsavel responsavel, string correlationId, Guid? usuarioId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Responsavel>> ListarPorClienteTitularIdAsync(Guid clienteTitularId, CancellationToken cancellationToken);
}
