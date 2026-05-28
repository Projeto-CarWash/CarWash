using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Interfaces;
using CarWash.Application.Servicos.Common;
using Microsoft.EntityFrameworkCore;

namespace CarWash.Application.Servicos.Atualizar;

public sealed class AtualizarServicoHandler : ICommandHandler<AtualizarServicoCommand, ServicoResponse>
{
    private readonly ICarWashDbContext _context;

    public AtualizarServicoHandler(ICarWashDbContext context)
    {
        _context = context;
    }

    public async Task<ServicoResponse> HandleAsync(AtualizarServicoCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var servico = await _context.Servicos
            .FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (servico is null)
        {
            throw new NotFoundException("Serviço não encontrado.");
        }

        if (!command.Preco.HasValue || !command.DuracaoMin.HasValue)
        {
            throw new ValidationException(
                "Dados do serviço inválidos. Verifique os campos e tente novamente.",
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["preco"] = ["Preço é obrigatório."],
                    ["duracaoMin"] = ["Duração é obrigatória."]
                });
        }

        var nomeSanitizado = command.Nome?.Trim() ?? string.Empty;

        // Check duplicate name for other service
        var existeNomeOutro = await _context.Servicos
            .AnyAsync(s => s.Nome.ToLower() == nomeSanitizado.ToLower() && s.Id != command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (existeNomeOutro)
        {
            throw new ConflictException("Já existe um serviço cadastrado com este nome.", "servico_nome_duplicado");
        }

        servico.Atualizar(nomeSanitizado, command.Preco.Value, command.DuracaoMin.Value);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ServicoResponse.FromEntity(servico);
    }
}
