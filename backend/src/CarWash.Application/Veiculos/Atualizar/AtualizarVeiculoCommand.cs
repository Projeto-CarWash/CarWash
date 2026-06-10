using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Veiculos.Common;

namespace CarWash.Application.Veiculos.Atualizar;

/// <summary>
/// Comando de atualização completa de veículo (PUT). Todos os campos são obrigatórios.
/// <c>TraceId</c> e <c>UsuarioId</c> são preenchidos pelo endpoint.
/// </summary>
public sealed record AtualizarVeiculoCommand(
    Guid VeiculoId,
    Guid ClienteId,
    string? Placa,
    string? Modelo,
    string? Fabricante,
    string? Cor,
    int? Ano,
    string TraceId,
    Guid? UsuarioId) : ICommand<VeiculoResponse>;
