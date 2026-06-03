using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Filiais.Common;

namespace CarWash.Application.Filiais.AlterarCelulasAtivas;

/// <summary>
/// Comando de alteração da quantidade de células ativas de uma filial (RF018).
/// Idempotente no handler: se o valor atual já bater com o solicitado, o
/// handler devolve o estado sem salvar nem auditar (mesmo padrão de
/// <see cref="Usuarios.AlterarStatus.AlterarStatusUsuarioHandler"/>).
/// </summary>
public sealed record AlterarCelulasAtivasCommand(Guid FilialId, int? CelulasAtivas)
    : ICommand<FilialResponse>;
