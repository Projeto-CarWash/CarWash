using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Filiais.Common;

namespace CarWash.Application.Filiais.Criar;

/// <summary>
/// Comando de cadastro de filial (RF017 + RF018). <c>TraceId</c> e
/// <c>UsuarioId</c> são preenchidos pelo endpoint a partir do
/// <see cref="HttpContext"/>. A filial nasce sempre ativa (ADR-0007 §2.3).
/// </summary>
public sealed record CriarFilialCommand(
    string? Nome,
    string? Codigo,
    string? Cnpj,
    int? CelulasAtivas,
    string? Timezone,
    EnderecoFilialRequest? Endereco,
    string TraceId,
    Guid? UsuarioId) : ICommand<CriarFilialResponse>;
