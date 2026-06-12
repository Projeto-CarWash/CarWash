using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Responsaveis.Common;
using CarWash.Application.Responsaveis.Persistence;

namespace CarWash.Application.Responsaveis.AlterarStatus;

public sealed class AlterarStatusResponsavelHandler
    : ICommandHandler<AlterarStatusResponsavelCommand, ResponsavelResponse>
{
    private readonly IResponsavelRepository _repositorio;

    public AlterarStatusResponsavelHandler(IResponsavelRepository repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<ResponsavelResponse> HandleAsync(AlterarStatusResponsavelCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool ativo = command.Ativo!.Value;

        var responsavel = await _repositorio.ObterPorIdRastreadoAsync(command.ResponsavelId, command.ClienteTitularId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Responsável não encontrado.");

        if (ativo)
        {
            responsavel.Ativar();
        }
        else
        {
            responsavel.Inativar();
        }

        await _repositorio.SalvarAsync(command.TraceId, command.UsuarioId, cancellationToken).ConfigureAwait(false);

        return ResponsavelResponse.FromEntity(responsavel);
    }
}
