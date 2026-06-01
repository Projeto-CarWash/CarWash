using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Clientes.Common;
using CarWash.Application.Clientes.Persistence;
using CarWash.Application.Common.Exceptions;

namespace CarWash.Application.Clientes.AlterarStatus;

/// <summary>
/// Use case de ativar/inativar cliente. <c>PATCH /api/v1/clientes/{id}/status</c>.
/// </summary>
public sealed class AlterarStatusClienteHandler
    : ICommandHandler<AlterarStatusClienteCommand, ClienteResponse>
{
    private readonly IClienteRepository _repositorio;

    public AlterarStatusClienteHandler(IClienteRepository repositorio)
    {
        _repositorio = repositorio;
    }

    /// <inheritdoc/>
    public async Task<ClienteResponse> HandleAsync(AlterarStatusClienteCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Validator garante NotNull antes de chegar aqui. `.Value` é seguro.
        bool ativo = command.Ativo!.Value;

        var cliente = await _repositorio.ObterPorIdAsync(command.ClienteId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Cliente não encontrado.");

        if (ativo)
        {
            cliente.Ativar();
        }
        else
        {
            cliente.Inativar();
        }

        // GAP-CW-CLI-AUDIT: status também conta como alteração — registra o ator.
        cliente.RegistrarAtualizadoPor(command.UsuarioId);

        await _repositorio.SalvarAsync(cancellationToken).ConfigureAwait(false);
        return ClienteResponse.FromEntity(cliente);
    }
}
