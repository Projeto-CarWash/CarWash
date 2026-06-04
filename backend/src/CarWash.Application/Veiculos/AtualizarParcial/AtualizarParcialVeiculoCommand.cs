using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Veiculos.Common;

namespace CarWash.Application.Veiculos.AtualizarParcial;

/// <summary>
/// Comando de atualização parcial de veículo (PATCH). Apenas campos enviados são alterados.
/// Pelo menos 1 campo deve estar presente. <c>TraceId</c> preenchido pelo endpoint.
/// </summary>
public sealed record AtualizarParcialVeiculoCommand(
    Guid VeiculoId,
    Guid ClienteId,
    string? Placa,
    string? Modelo,
    string? Fabricante,
    string? Cor,
    string TraceId,
    Guid? UsuarioId) : ICommand<VeiculoAtualizadoResponse>;
