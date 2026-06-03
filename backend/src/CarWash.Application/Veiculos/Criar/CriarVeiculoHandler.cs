using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Clientes.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Veiculos.Common;
using CarWash.Application.Veiculos.Persistence;
using CarWash.Domain.Common;
using CarWash.Domain.Entities;
using CarWash.Domain.ValueObjects;

namespace CarWash.Application.Veiculos.Criar;

/// <summary>
/// Use case de cadastro de veículo (RF005). Defesa em camadas para a placa:
/// validator (sintaxe) → value object <see cref="Placa"/> (RN003 / formato) →
/// pré-check no banco (RN011) → UK <c>uk_veiculos_placa</c>
/// (race condition concorrente). Normalização (trim + uppercase) aplicada
/// antes de qualquer validação.
/// </summary>
public sealed class CriarVeiculoHandler : ICommandHandler<CriarVeiculoCommand, VeiculoResponse>
{
    private readonly IClienteRepository _clientes;
    private readonly IVeiculoRepository _veiculos;

    public CriarVeiculoHandler(IClienteRepository clientes, IVeiculoRepository veiculos)
    {
        _clientes = clientes;
        _veiculos = veiculos;
    }

    /// <inheritdoc/>
    public async Task<VeiculoResponse> HandleAsync(CriarVeiculoCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var cliente = await _clientes.ObterPorIdAsync(command.ClienteId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Cliente não encontrado.");

        // RN003 + RAT03: normaliza (trim + uppercase) antes de instanciar o value object.
        // O value object valida o formato Mercosul/antigo antes do hit no banco.
        var placaNormalizada = (command.Placa ?? string.Empty).Trim().ToUpperInvariant();
        var placa = new Placa(placaNormalizada);

        if (await _veiculos.ExistePlacaAsync(placa.Valor, cancellationToken).ConfigureAwait(false))
        {
            throw new PlacaJaCadastradaException();
        }

        var veiculo = Veiculo.Criar(
            id: Guid.NewGuid(),
            clienteId: cliente.Id,
            placa: placa,
            modelo: command.Modelo!,
            fabricante: command.Fabricante!,
            cor: command.Cor!,
            ano: command.Ano);

        // Defesa via invariante do agregado: cliente inativo → DomainException,
        // que o handler traduz para RecursoInativoException (422) — decisão do
        // arquiteto (2026-05-25). 409 fica reservado para placa duplicada.
        try
        {
            cliente.AdicionarVeiculo(veiculo);
        }
        catch (DomainException ex) when (ex.Message.Contains("inativo", StringComparison.OrdinalIgnoreCase))
        {
            throw new RecursoInativoException(ex.Message, ex);
        }

        await _veiculos.AdicionarAsync(veiculo, cancellationToken).ConfigureAwait(false);

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
