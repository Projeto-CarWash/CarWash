using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Servicos.Common;

namespace CarWash.Application.Servicos.AlterarStatus;

public sealed record AlterarStatusServicoCommand(
    Guid Id,
    bool Ativo,
    Guid? UsuarioId) : ICommand<ServicoResponse>;
