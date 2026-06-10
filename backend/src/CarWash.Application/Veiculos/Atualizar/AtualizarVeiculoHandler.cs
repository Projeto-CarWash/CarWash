using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Clientes.Persistence;
using CarWash.Application.Common;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Veiculos.Common;
using CarWash.Application.Veiculos.Persistence;
using CarWash.Domain.ValueObjects;

namespace CarWash.Application.Veiculos.Atualizar;

/// <summary>
/// Use case de atualização completa de veículo. <c>PUT /api/v1/clientes/{clienteId}/veiculos/{id}</c>.
/// Todos os campos são obrigatórios (substituição completa). Placa única global
/// com exceção do próprio veículo.
/// </summary>
public sealed class AtualizarVeiculoHandler : ICommandHandler<AtualizarVeiculoCommand, VeiculoResponse>
{
    private readonly IClienteRepository _clientes;
    private readonly IVeiculoRepository _veiculos;

    public AtualizarVeiculoHandler(IClienteRepository clientes, IVeiculoRepository veiculos)
    {
        _clientes = clientes;
        _veiculos = veiculos;
    }

    public async Task<VeiculoResponse> HandleAsync(AtualizarVeiculoCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        _ = await _clientes.ObterPorIdAsync(command.ClienteId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Cliente não encontrado.");

        var veiculo = await _veiculos.ObterPorIdAsync(command.VeiculoId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Veículo não encontrado.");

        if (veiculo.ClienteId != command.ClienteId)
        {
            throw new NotFoundException("Veículo não encontrado.");
        }

        string placaNormalizada = InputNormalizer.PlacaOrNull(command.Placa)!;
        var placa = new Placa(placaNormalizada);

        if (await _veiculos.ExistePlacaExcetoAsync(placa.Valor, command.VeiculoId, cancellationToken).ConfigureAwait(false))
        {
            throw new PlacaJaCadastradaException();
        }

        veiculo.Atualizar(
            placa: placa,
            modelo: command.Modelo!,
            fabricante: command.Fabricante!,
            cor: command.Cor!,
            ano: command.Ano);

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
