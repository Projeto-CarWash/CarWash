using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Clientes.Common;

namespace CarWash.Application.Clientes.AlterarStatus;

/// <summary>
/// Comando de alteração de status (ativar/inativar) de cliente.
/// <para>
/// <c>Ativo</c> é nullable para preservar a distinção entre "ausente no body" e
/// <c>false</c> (GAP-CW-CLI-STA-EMP). O validator exige <c>NotNull</c>; o handler
/// propaga <c>.Value</c>.
/// </para>
/// </summary>
public sealed record AlterarStatusClienteCommand(Guid ClienteId, bool? Ativo, Guid? UsuarioId)
    : ICommand<ClienteResponse>;
