using CarWash.Application.Agendamentos.Persistence;
using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarWash.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação concreta de <see cref="IIdempotenciaRepository"/> sobre EF Core
/// — leitura dos registros de idempotência (RF015). A escrita do registro é feita
/// pelo <see cref="AgendamentoRepository"/> na mesma transação da confirmação.
/// </summary>
public sealed class IdempotenciaRepository : IIdempotenciaRepository
{
    private readonly CarWashDbContext _db;

    public IdempotenciaRepository(CarWashDbContext db)
    {
        _db = db;
    }

    public Task<IdempotenciaRequisicao?> ObterAsync(
        Guid idempotencyKey,
        string escopo,
        CancellationToken cancellationToken)
    {
        return _db.IdempotenciaRequisicoes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.IdempotencyKey == idempotencyKey && r.Escopo == escopo,
                cancellationToken);
    }
}
