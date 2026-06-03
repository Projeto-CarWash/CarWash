using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Veiculos.Common;

namespace CarWash.Application.Veiculos.CriarBatch;

/// <summary>
/// Comando de cadastro em batch de veículos vinculados a um cliente existente (RF005).
/// <c>ClienteId</c> vem da rota; <c>TraceId</c> e <c>UsuarioId</c> são preenchidos
/// pelo endpoint a partir do <see cref="HttpContext"/>.
/// </summary>
public sealed record CriarVeiculosBatchCommand(
    Guid ClienteId,
    IReadOnlyList<VeiculoItemCommand> Veiculos,
    string TraceId,
    Guid? UsuarioId) : ICommand<IReadOnlyList<VeiculoResponse>>;

/// <summary>
/// Item individual do comando de batch.
/// </summary>
public sealed record VeiculoItemCommand(
    string? Placa,
    string? Modelo,
    string? Fabricante,
    string? Cor,
    int? Ano);
