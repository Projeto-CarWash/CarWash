using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Clientes.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Veiculos.Common;
using CarWash.Application.Veiculos.Persistence;
using CarWash.Domain.ValueObjects;

namespace CarWash.Application.Veiculos.Atualizar;

/// <summary>
/// Use case de atualização completa de veículo. <c>PUT /api/v1/clientes/{clienteId}/veiculos/{veiculoId}</c>.
/// Todos os campos obrigatórios (substituição completa). Placa única global
/// com exceção do próprio veículo — mesma placa do próprio veículo não gera falso conflito.
/// </summary>
public sealed class AtualizarVeiculoHandler : ICommandHandler<AtualizarVeiculoCommand, VeiculoAtualizadoResponse>
{
    private readonly IClienteRepository _clientes;
    private readonly IVeiculoRepository _veiculos;

    public AtualizarVeiculoHandler(IClienteRepository clientes, IVeiculoRepository veiculos)
    {
        _clientes = clientes;
        _veiculos = veiculos;
    }

    public async Task<VeiculoAtualizadoResponse> HandleAsync(AtualizarVeiculoCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var cliente = await _clientes.ObterPorIdAsync(command.ClienteId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Cliente não encontrado.");

        var veiculo = await _veiculos.ObterPorIdAsync(command.VeiculoId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Veículo não encontrado.");

        if (veiculo.ClienteId != command.ClienteId)
        {
            throw new NotFoundException("Veículo não encontrado.");
        }

        var placaNormalizada = (command.Placa ?? string.Empty).Trim().ToUpperInvariant();
        var placa = new Placa(placaNormalizada);

        if (placa.Valor != veiculo.Placa
            && await _veiculos.ExistePlacaExcetoAsync(placa.Valor, command.VeiculoId, cancellationToken).ConfigureAwait(false))
        {
            throw new PlacaJaCadastradaException();
        }

        veiculo.AtualizarDados(
            placa: placa,
            modelo: command.Modelo!,
            fabricante: command.Fabricante!,
            cor: command.Cor!);

        await _veiculos.SalvarAsync(cancellationToken).ConfigureAwait(false);

        return new VeiculoAtualizadoResponse
        {
            Message = "Veículo atualizado com sucesso.",
            TraceId = command.TraceId,
            Data = new VeiculoAtualizadoData
            {
                Id = veiculo.Id,
                ClienteId = veiculo.ClienteId,
                Placa = veiculo.Placa,
                Modelo = veiculo.Modelo,
                Fabricante = veiculo.Fabricante,
                Cor = veiculo.Cor,
                Ano = veiculo.Ano,
                Ativo = veiculo.Ativo,
                AtualizadoEm = veiculo.AtualizadoEm,
            },
        };
    }
}
