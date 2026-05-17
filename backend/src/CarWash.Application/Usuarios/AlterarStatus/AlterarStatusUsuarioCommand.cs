using CarWash.Application.Abstractions.Messaging;

namespace CarWash.Application.Usuarios.AlterarStatus;

/// <summary>
/// Comando de alteração de status (ativar/inativar) de usuário interno. Idempotente:
/// se o estado atual já bater com o solicitado, o handler retorna o estado sem
/// salvar nem auditar.
/// <para>
/// <c>Ativo</c> é nullable para preservar a distinção entre "ausente no body" e
/// <c>false</c> (BUG-U004). O validator exige <c>NotNull</c>; o handler propaga
/// <c>.Value</c>.
/// </para>
/// </summary>
public sealed record AlterarStatusUsuarioCommand(Guid UsuarioId, bool? Ativo)
    : ICommand<AlterarStatusUsuarioResponse>;
