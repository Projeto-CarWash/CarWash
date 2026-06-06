using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Clientes.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Veiculos.Common;
using CarWash.Application.Veiculos.Persistence;

namespace CarWash.Application.Veiculos.AlterarStatus;

/// <summary>
/// Use case de ativar/inativar veículo. <c>PATCH /api/v1/clientes/{clienteId}/veiculos/{id}/status</c>.
/// </summary>
public sealed class AlterarStatusVeiculoHandler
    : ICommandHandler<AlterarStatusVeiculoCommand, VeiculoResponse>
{
    private readonly IClienteRepository _clientes;
    private readonly IVeiculoRepository _veiculos;

    public AlterarStatusVeiculoHandler(IClienteRepository clientes, IVeiculoRepository veiculos)
    {
        _clientes = clientes;
        _veiculos = veiculos;
    }

    public async Task<VeiculoResponse> HandleAsync(AlterarStatusVeiculoCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool ativo = command.Ativo!.Value;

        _ = await _clientes.ObterPorIdAsync(command.ClienteId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Cliente não encontrado.");

        var veiculo = await _veiculos.ObterPorIdAsync(command.VeiculoId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Veículo não encontrado.");

        if (veiculo.ClienteId != command.ClienteId)
        {
            throw new NotFoundException("Veículo não encontrado.");
        }

        if (ativo)
        {
            veiculo.Ativar();
        }
        else
        {
            veiculo.Inativar();
        }

        await _veiculos.SalvarAsync(cancellationToken).ConfigureAwait(false);

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
