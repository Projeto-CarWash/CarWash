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
}
