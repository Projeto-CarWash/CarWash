using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Clientes.Common;
using CarWash.Application.Clientes.Persistence;
using CarWash.Application.Common.Exceptions;

namespace CarWash.Application.Clientes.ObterPorId;

public sealed class ObterClientePorIdHandler : IQueryHandler<ObterClientePorIdQuery, ClienteResponse>
{
    public const string MensagemNaoEncontrado = "Cliente não encontrado.";

    private readonly IClienteRepository _repositorio;

    public ObterClientePorIdHandler(IClienteRepository repositorio)
    {
        _repositorio = repositorio;
    }

    /// <inheritdoc/>
    public async Task<ClienteResponse> HandleAsync(ObterClientePorIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var cliente = await _repositorio.ObterPorIdAsync(query.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(MensagemNaoEncontrado);

        return ClienteResponse.FromEntity(cliente);
    }
}
