using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Interfaces;
using CarWash.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarWash.Application.Servicos.Criar;

public sealed class CriarServicoHandler : ICommandHandler<CriarServicoCommand, CriarServicoResponse>
{
    private readonly ICarWashDbContext _context;

    public CriarServicoHandler(ICarWashDbContext context)
    {
        _context = context;
    }

    public async Task<CriarServicoResponse> HandleAsync(CriarServicoCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

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

        // Check for duplicate name
        var existeNome = await _context.Servicos
            .AnyAsync(s => s.Nome.ToLower() == nomeSanitizado.ToLower(), cancellationToken)
            .ConfigureAwait(false);

        if (existeNome)
        {
            throw new ConflictException("Já existe um serviço cadastrado com este nome.", "servico_nome_duplicado");
        }

        var servico = Servico.Criar(
            id: Guid.NewGuid(),
            nome: nomeSanitizado,
            preco: command.Preco.Value,
            duracaoMin: command.DuracaoMin.Value);

        _context.Servicos.Add(servico);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CriarServicoResponse
        {
            Id = servico.Id,
            Mensagem = "Serviço cadastrado com sucesso.",
            TraceId = command.TraceId
        };
    }
}
