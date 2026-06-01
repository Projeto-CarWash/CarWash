using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Clientes.Common;
using CarWash.Application.Clientes.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace CarWash.Application.Clientes.ObterPorId;

public sealed class ObterClientePorIdHandler : IQueryHandler<ObterClientePorIdQuery, ClienteResponse>
{
    public const string MensagemNaoEncontrado = "Cliente não encontrado.";

    private readonly IClienteRepository _repositorio;
    private readonly ICarWashDbContext _context;

    public ObterClientePorIdHandler(IClienteRepository repositorio, ICarWashDbContext context)
    {
        _repositorio = repositorio;
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<ClienteResponse> HandleAsync(ObterClientePorIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var cliente = await _repositorio.ObterPorIdAsync(query.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(MensagemNaoEncontrado);

        var response = ClienteResponse.FromEntity(cliente);

        var veiculos = await _context.Veiculos
            .AsNoTracking()
            .Where(v => v.ClienteId == query.Id && v.Ativo)
            .Select(v => new ClienteResponse.ClienteVeiculoResponse
            {
                Id = v.Id,
                Placa = v.Placa,
                Modelo = v.Modelo,
                Fabricante = v.Fabricante,
                Cor = v.Cor
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        response.Veiculos = veiculos;

        return response;
    }
}
