using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Clientes.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Veiculos.Common;
using CarWash.Application.Veiculos.Persistence;

namespace CarWash.Application.Veiculos.ObterPorId;

public sealed class ObterVeiculoPorIdHandler : IQueryHandler<ObterVeiculoPorIdQuery, VeiculoResponse>
{
    public const string MensagemNaoEncontrado = "Veículo não encontrado.";

    private readonly IClienteRepository _clientes;
    private readonly IVeiculoRepository _veiculos;

    public ObterVeiculoPorIdHandler(IClienteRepository clientes, IVeiculoRepository veiculos)
    {
        _clientes = clientes;
        _veiculos = veiculos;
    }

    public async Task<VeiculoResponse> HandleAsync(ObterVeiculoPorIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var cliente = await _clientes.ObterPorIdAsync(query.ClienteId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Cliente não encontrado.");

        var veiculo = await _veiculos.ObterPorIdAsync(query.VeiculoId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(MensagemNaoEncontrado);

        if (veiculo.ClienteId != query.ClienteId)
        {
            throw new NotFoundException(MensagemNaoEncontrado);
        }

        return new VeiculoResponse
        {
            Id = veiculo.Id,
            ClienteId = veiculo.ClienteId,
            Placa = veiculo.Placa,
            Modelo = veiculo.Modelo,
            Fabricante = veiculo.Fabricante,
            Cor = veiculo.Cor,
            Ano = veiculo.Ano,
            Ativo = veiculo.Ativo,
            CriadoEm = veiculo.CriadoEm,
            AtualizadoEm = veiculo.AtualizadoEm,
        };
    }
}
