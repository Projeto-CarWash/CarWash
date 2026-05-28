using CarWash.Application.Abstractions.Messaging;

namespace CarWash.Application.Servicos.Criar;

public sealed record CriarServicoCommand(
    string? Nome,
    decimal? Preco,
    int? DuracaoMin,
    string TraceId,
    Guid? UsuarioId) : ICommand<CriarServicoResponse>;
