using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Servicos.Common;

namespace CarWash.Application.Servicos.Criar;

/// <summary>
/// Comando de cadastro de serviço (RF006). Nome único, preço e duração
/// obrigatórios. <c>TraceId</c> e <c>UsuarioId</c> são preenchidos pelo
/// endpoint a partir do <see cref="HttpContext"/>.
/// </summary>
public sealed record CriarServicoCommand(
    string? Nome,
    decimal? Preco,
    int? DuracaoMin,
    string TraceId,
    Guid? UsuarioId) : ICommand<CriarServicoResponse>;
