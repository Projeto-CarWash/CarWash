using CarWash.Domain.Entities;

namespace CarWash.Application.Agendamentos.Persistence;

/// <summary>
/// Porta de leitura dos registros de idempotência (RF015). A escrita do registro
/// é feita na mesma transação da confirmação via
/// <see cref="IAgendamentoRepository.AdicionarComIdempotenciaAsync"/> — manter a
/// gravação fora daqui garante atomicidade entre o agendamento e o registro.
/// </summary>
public interface IIdempotenciaRepository
{
    /// <summary>
    /// Localiza um registro de idempotência pela chave + escopo. Retorna
    /// <c>null</c> quando a chave nunca foi usada nesse escopo.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    Task<IdempotenciaRequisicao?> ObterAsync(
        Guid idempotencyKey,
        string escopo,
        CancellationToken cancellationToken);
}
