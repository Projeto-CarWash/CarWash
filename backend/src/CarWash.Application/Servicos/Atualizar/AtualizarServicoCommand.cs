using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Servicos.Common;

namespace CarWash.Application.Servicos.Atualizar;

/// <summary>
/// Comando de atualização de serviço (RF006 — campos editáveis).
/// <c>TraceId</c> e <c>UsuarioId</c> preenchidos pelo endpoint.
/// </summary>
public sealed record AtualizarServicoCommand(
    Guid Id,
    string? Nome,
    decimal? Preco,
    int? DuracaoMin,
    string TraceId,
    Guid? UsuarioId) : ICommand<ServicoResponse>;
