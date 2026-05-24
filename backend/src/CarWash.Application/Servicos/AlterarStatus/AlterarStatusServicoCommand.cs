using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Servicos.Common;

namespace CarWash.Application.Servicos.AlterarStatus;

/// <summary>
/// Comando de alteração de status (ativar/inativar) de serviço.
/// <para>
/// <c>Ativo</c> é nullable para preservar a distinção entre "ausente no body" e
/// <c>false</c>. O validator exige <c>NotNull</c>; o handler
/// propaga <c>.Value</c>. <c>TraceId</c> e <c>UsuarioId</c> preenchidos pelo endpoint.
/// </para>
/// </summary>
public sealed record AlterarStatusServicoCommand(Guid ServicoId, bool? Ativo, string TraceId, Guid? UsuarioId)
    : ICommand<ServicoResponse>;
