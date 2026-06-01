using CarWash.Application.Clientes.HistoricoAtendimentos.Common;

namespace CarWash.Application.Interfaces;

public interface IHistoricoAtendimentosClienteRepository
{
    Task<bool> ClienteExisteAsync(
        Guid clienteId,
        CancellationToken cancellationToken);

    Task<(IReadOnlyCollection<HistoricoAtendimentoResponse> Itens, int Total)> ConsultarAsync(
        Guid clienteId,
        DateTimeOffset? dataInicio,
        DateTimeOffset? dataFim,
        int? ultimosDias,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
