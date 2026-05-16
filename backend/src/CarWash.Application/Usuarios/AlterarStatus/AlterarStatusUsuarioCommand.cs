using CarWash.Application.Abstractions.Messaging;

namespace CarWash.Application.Usuarios.AlterarStatus;

/// <summary>
/// Comando de alteração de status (ativar/inativar) de usuário interno. Idempotente:
/// se o estado atual já bater com o solicitado, o handler retorna o estado sem
/// salvar nem auditar.
/// </summary>
public sealed record AlterarStatusUsuarioCommand(Guid UsuarioId, bool Ativo)
    : ICommand<AlterarStatusUsuarioResponse>;
