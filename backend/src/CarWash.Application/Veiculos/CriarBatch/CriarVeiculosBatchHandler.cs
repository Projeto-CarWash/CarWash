using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Clientes.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Veiculos.Common;
using CarWash.Application.Veiculos.Persistence;
using CarWash.Domain.Common;
using CarWash.Domain.Entities;
using CarWash.Domain.ValueObjects;

namespace CarWash.Application.Veiculos.CriarBatch;

/// <summary>
/// Use case de cadastro em batch de veículos (RF005). Valida todos os itens,
/// verifica duplicidade no payload e no banco, e persiste dentro de uma
/// transação única — rollback integral se qualquer item falhar.
/// </summary>
public sealed class CriarVeiculosBatchHandler : ICommandHandler<CriarVeiculosBatchCommand, IReadOnlyList<VeiculoResponse>>
{
    private readonly IClienteRepository _clientes;
    private readonly IVeiculoRepository _veiculos;

    public CriarVeiculosBatchHandler(IClienteRepository clientes, IVeiculoRepository veiculos)
    {
        _clientes = clientes;
        _veiculos = veiculos;
    }

    public async Task<IReadOnlyList<VeiculoResponse>> HandleAsync(
        CriarVeiculosBatchCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var cliente = await _clientes.ObterPorIdAsync(command.ClienteId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Cliente não encontrado.");

        // 1) Construir value objects de placa (normaliza + valida formato).
        //    Se qualquer placa for inválida, DomainException antes de tocar no banco.
        var placas = new List<Placa>(command.Veiculos.Count);
        foreach (var item in command.Veiculos)
        {
            placas.Add(new Placa(item.Placa ?? string.Empty));
        }

        // 2) Duplicidade no payload — já validada pelo validator, mas reforçamos
        //    aqui como defesa em profundidade (caso o handler seja chamado diretamente).
        var placasNormalizadas = placas.Select(p => p.Valor).ToList();
        if (placasNormalizadas.Distinct().Count() != placasNormalizadas.Count)
        {
            throw new PlacaDuplicadaPayloadException();
        }

        // 3) Pré-check de duplicidade no banco (RN011).
        var existentes = await _veiculos.PlacasExistentesAsync(placasNormalizadas, cancellationToken).ConfigureAwait(false);
        if (existentes.Count > 0)
        {
            throw new PlacaJaCadastradaException();
        }

        // 4) Construir entidades de domínio — falha rápida se algum campo for inválido.
        var veiculos = new List<Veiculo>(command.Veiculos.Count);
        for (var i = 0; i < command.Veiculos.Count; i++)
        {
            var item = command.Veiculos[i];
            var veiculo = Veiculo.Criar(
                id: Guid.NewGuid(),
                clienteId: cliente.Id,
                placa: placas[i],
                modelo: item.Modelo!,
                fabricante: item.Fabricante!,
                cor: item.Cor!,
                ano: item.Ano);

            // Cliente inativo não pode receber veículos
            try
            {
                cliente.AdicionarVeiculo(veiculo);
            }
            catch (DomainException ex) when (ex.Message.Contains("inativo", StringComparison.OrdinalIgnoreCase))
            {
                throw new RecursoInativoException(ex.Message, ex);
            }

            veiculos.Add(veiculo);
        }

        // 5) Persistir tudo em uma transação única — rollback integral se falhar.
        await _veiculos.AdicionarRangeAsync(veiculos, cancellationToken).ConfigureAwait(false);

        // 6) Montar responses.
        return veiculos.Select(v => new VeiculoResponse
        {
            Id = v.Id,
            ClienteId = v.ClienteId,
            Placa = v.Placa,
            Modelo = v.Modelo,
            Fabricante = v.Fabricante,
            Cor = v.Cor,
            Ano = v.Ano,
            Ativo = v.Ativo,
            CriadoEm = v.CriadoEm,
            AtualizadoEm = v.AtualizadoEm,
        }).ToList();
    }
}
