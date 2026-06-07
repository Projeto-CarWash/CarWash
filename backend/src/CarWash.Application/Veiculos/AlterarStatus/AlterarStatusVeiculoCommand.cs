using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Veiculos.Common;

namespace CarWash.Application.Veiculos.AlterarStatus;

/// <summary>
/// Comando de alteração de status (ativar/inativar) de veículo.
/// <c>Ativo</c> é nullable para preservar a distinção entre "ausente no body" e
/// <c>false</c>. O validator exige <c>NotNull</c>.
/// </summary>
public sealed record AlterarStatusVeiculoCommand(
    Guid ClienteId,
    Guid VeiculoId,
    bool? Ativo,
    string TraceId,
    Guid? UsuarioId) : ICommand<VeiculoResponse>;
