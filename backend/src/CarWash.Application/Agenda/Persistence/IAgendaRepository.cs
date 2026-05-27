using CarWash.Application.Agenda.Common;

namespace CarWash.Application.Agenda.Persistence;

/// <summary>
/// Porta de leitura da agenda (RF009). A implementação concreta vive em
/// <c>CarWash.Infrastructure</c> e projeta uma única query EF Core para
/// <see cref="AgendaProjecao"/> — mantém a Application desacoplada do EF Core.
/// </summary>
public interface IAgendaRepository
{
    /// <summary>
    /// Consulta os eventos da agenda da filial dentro da janela
    /// <c>[inicioUtc, fimUtc)</c>, com filtros opcionais por cliente, responsável
    /// e status (já mapeado para o valor persistido no banco). Resultado ordenado
    /// por <c>Inicio ASC, CriadoEm ASC</c>.
    /// </summary>
    /// <param name="filialId">Filial obrigatória do agendamento.</param>
    /// <param name="inicioUtc">Limite inferior (inclusivo) da janela.</param>
    /// <param name="fimUtc">Limite superior (exclusivo) da janela.</param>
    /// <param name="clienteId">Filtro opcional por cliente titular.</param>
    /// <param name="responsavelId">Filtro opcional por responsável de execução.</param>
    /// <param name="statusDb">Filtro opcional por status persistido no banco.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<IReadOnlyList<AgendaProjecao>> ConsultarAsync(
        Guid filialId,
        DateTime inicioUtc,
        DateTime fimUtc,
        Guid? clienteId,
        Guid? responsavelId,
        string? statusDb,
        CancellationToken cancellationToken);
}
