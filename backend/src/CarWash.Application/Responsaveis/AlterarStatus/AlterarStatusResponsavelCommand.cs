using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Responsaveis.Common;

namespace CarWash.Application.Responsaveis.AlterarStatus;

public sealed record AlterarStatusResponsavelCommand(
    Guid ResponsavelId,
    Guid ClienteTitularId,
    bool? Ativo,
    string TraceId,
    Guid? UsuarioId) : ICommand<ResponsavelResponse>;
