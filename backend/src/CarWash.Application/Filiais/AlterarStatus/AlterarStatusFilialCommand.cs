using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Filiais.Common;

namespace CarWash.Application.Filiais.AlterarStatus;

/// <summary>
/// Comando de alteração de status (ativar/inativar) de filial — RF017.
/// Filial inativa deixa de aceitar novos agendamentos (RF019: criação com
/// filial inativa → 409 <c>filial-inativa</c>). Idempotente: se o estado
/// atual já bater com o solicitado, o handler retorna sem salvar nem auditar.
/// </summary>
public sealed record AlterarStatusFilialCommand(Guid FilialId, bool? Ativo)
    : ICommand<FilialResponse>;
