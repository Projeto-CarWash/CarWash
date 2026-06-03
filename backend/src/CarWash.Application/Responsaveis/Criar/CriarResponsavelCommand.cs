using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Responsaveis.Criar;

namespace CarWash.Application.Responsaveis.Criar;

public sealed record CriarResponsavelCommand(
    Guid ClienteTitularId,
    string? Nome,
    string? Documento,
    string? Telefone,
    string? Email,
    string? GrauVinculo,
    string TraceId,
    Guid? UsuarioId) : ICommand<CriarResponsavelResponse>;
