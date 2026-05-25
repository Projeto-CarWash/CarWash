using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Veiculos.Common;

namespace CarWash.Application.Veiculos.Criar;

/// <summary>
/// Comando de cadastro de veículo vinculado a um cliente existente (RF005).
/// <c>ClienteId</c> vem da rota; <c>TraceId</c> e <c>UsuarioId</c> são preenchidos
/// pelo endpoint a partir do <see cref="HttpContext"/>.
/// </summary>
public sealed record CriarVeiculoCommand(
    Guid ClienteId,
    string? Placa,
    string? Modelo,
    string? Fabricante,
    string? Cor,
    int? Ano,
    string TraceId,
    Guid? UsuarioId) : ICommand<VeiculoResponse>;
