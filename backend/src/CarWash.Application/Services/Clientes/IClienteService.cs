using CarWash.Application.DTOs.Clientes;

namespace CarWash.Application.Services.Clientes;

public interface IClienteService
{
    Task<CreateClienteResponse> CriarAsync(
        CreateClienteRequest request,
        string traceId,
        Guid? usuarioId,
        CancellationToken cancellationToken);

    Task<ClienteResponse?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ClienteResponse> AtualizarAsync(
        Guid id,
        UpdateClienteRequest request,
        Guid? usuarioId,
        CancellationToken cancellationToken);

    Task<ClienteResponse> AlterarStatusAsync(
        Guid id,
        bool ativo,
        Guid? usuarioId,
        CancellationToken cancellationToken);

    Task<ListaClientesResponse> ListarAsync(
        string? busca,
        bool? ativo,
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken);
}
