using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Filiais.Common;

namespace CarWash.Application.Filiais.CriarFilial;

/// <summary>
/// Comando de criação de filial (RF017/RF018). <c>CelulasAtivas</c> é nullable
/// para o validator capturar body sem o campo; o handler propaga <c>.Value</c>
/// após a validação.
/// </summary>
public sealed record CriarFilialCommand(string? Nome, int? CelulasAtivas, string? Timezone)
    : ICommand<FilialResponse>;
