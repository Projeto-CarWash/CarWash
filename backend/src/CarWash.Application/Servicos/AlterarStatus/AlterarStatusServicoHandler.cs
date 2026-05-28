using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Interfaces;
using CarWash.Application.Servicos.Common;
using Microsoft.EntityFrameworkCore;

namespace CarWash.Application.Servicos.AlterarStatus;

public sealed class AlterarStatusServicoHandler : ICommandHandler<AlterarStatusServicoCommand, ServicoResponse>
{
    private readonly ICarWashDbContext _context;

    public AlterarStatusServicoHandler(ICarWashDbContext context)
    {
        _context = context;
    }

    public async Task<ServicoResponse> HandleAsync(AlterarStatusServicoCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var servico = await _context.Servicos
            .FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (servico is null)
        {
            throw new NotFoundException("Serviço não encontrado.");
        }

        if (command.Ativo)
        {
            servico.Ativar();
        }
        else
        {
            servico.Inativar();
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ServicoResponse.FromEntity(servico);
    }
}
