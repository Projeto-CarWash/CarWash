using System.Text.Json;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Responsaveis.Common;

namespace CarWash.Application.Responsaveis.Atualizar;

public sealed record AtualizarResponsavelCommand(
    Guid ResponsavelId,
    Guid ClienteTitularId,
    string? Nome,
    string? Telefone,
    string? Email,
    string? GrauVinculo,
    Dictionary<string, JsonElement>? CamposExtras,
    string TraceId,
    Guid? UsuarioId) : ICommand<ResponsavelResponse>;
