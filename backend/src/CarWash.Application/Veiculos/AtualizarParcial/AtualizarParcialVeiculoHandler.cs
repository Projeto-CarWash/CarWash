using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Clientes.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Veiculos.Common;
using CarWash.Application.Veiculos.Persistence;
using CarWash.Domain.ValueObjects;

namespace CarWash.Application.Veiculos.AtualizarParcial;

/// <summary>
/// Use case de atualização parcial de veículo. <c>PATCH /api/v1/clientes/{clienteId}/veiculos/{veiculoId}</c>.
/// Apenas campos enviados são alterados. Placa: mesma do próprio veículo = não bloqueia;
/// placa de outro veículo = 409. Pelo menos 1 campo obrigatório.
/// </summary>
public sealed class AtualizarParcialVeiculoHandler : ICommandHandler<AtualizarParcialVeiculoCommand, VeiculoAtualizadoResponse>
{
    private readonly IClienteRepository _clientes;
    private readonly IVeiculoRepository _veiculos;

    public AtualizarParcialVeiculoHandler(IClienteRepository clientes, IVeiculoRepository veiculos)
    {
        _clientes = clientes;
        _veiculos = veiculos;
    }

    public async Task<VeiculoAtualizadoResponse> HandleAsync(AtualizarParcialVeiculoCommand command, CancellationToken cancellationToken)
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

        var placaFinal = veiculo.Placa;
        Placa? placaVo = null;

        if (!string.IsNullOrWhiteSpace(command.Placa))
        {
            var placaNormalizada = command.Placa.Trim().ToUpperInvariant();
            placaVo = new Placa(placaNormalizada);
            placaFinal = placaVo.Valor;

            if (placaVo.Valor != veiculo.Placa
                && await _veiculos.ExistePlacaExcetoAsync(placaVo.Valor, command.VeiculoId, cancellationToken).ConfigureAwait(false))
            {
                throw new PlacaJaCadastradaException();
            }
        }

        var modeloFinal = !string.IsNullOrWhiteSpace(command.Modelo) ? command.Modelo : veiculo.Modelo;
        var fabricanteFinal = !string.IsNullOrWhiteSpace(command.Fabricante) ? command.Fabricante : veiculo.Fabricante;
        var corFinal = !string.IsNullOrWhiteSpace(command.Cor) ? command.Cor : veiculo.Cor;

        placaVo ??= new Placa(placaFinal);

        veiculo.AtualizarDados(
            placa: placaVo,
            modelo: modeloFinal,
            fabricante: fabricanteFinal,
            cor: corFinal,
            ano: veiculo.Ano);

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
