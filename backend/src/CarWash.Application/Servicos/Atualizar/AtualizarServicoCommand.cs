using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Servicos.Common;

namespace CarWash.Application.Servicos.Atualizar;

public sealed record AtualizarServicoCommand(
    Guid Id,
    string? Nome,
    decimal? Preco,
    int? DuracaoMin,
    Guid? UsuarioId) : ICommand<ServicoResponse>;
