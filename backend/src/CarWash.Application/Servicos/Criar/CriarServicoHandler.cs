using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Common;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Servicos.Common;
using CarWash.Application.Servicos.Persistence;
using CarWash.Domain.Entities;

namespace CarWash.Application.Servicos.Criar;

/// <summary>
/// Use case de cadastro de serviço (RF006). Defesa em duas camadas para
/// unicidade de nome: pré-check + constraint UK no banco
/// (ConflictException emitida pelo repositório em violação concorrente).
/// </summary>
public sealed class CriarServicoHandler : ICommandHandler<CriarServicoCommand, CriarServicoResponse>
{
    private readonly IServicoRepository _repositorio;

    public CriarServicoHandler(IServicoRepository repositorio)
    {
        _repositorio = repositorio;
    }

    /// <inheritdoc/>
    public async Task<CriarServicoResponse> HandleAsync(CriarServicoCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Defesa em profundidade: o validator já exige NotNull em Preco/DuracaoMin,
        // mas se algum chamador interno bypassar a pipeline, falhamos com 400
        // estruturado em vez de 500 (NullReferenceException no Servico.Criar).
        if (!command.Preco.HasValue)
        {
            throw new ValidationException(
                "Dados do serviço inválidos. Verifique os campos e tente novamente.",
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["preco"] = ["O preço do serviço é obrigatório."],
                });
        }

        if (!command.DuracaoMin.HasValue)
        {
            throw new ValidationException(
                "Dados do serviço inválidos. Verifique os campos e tente novamente.",
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["duracaoMin"] = ["A duração do serviço é obrigatória."],
                });
        }

        string nome = InputNormalizer.SanitizeTextOrNull(command.Nome)!;

        // GAP-CW-SRV-NOME-1: nome deve ser único entre todos os serviços
        // (índice uk_servicos_nome no banco como defesa final).
        if (await _repositorio.ExisteNomeAsync(nome, ignoreServicoId: null, cancellationToken).ConfigureAwait(false))
        {
            throw new NomeServicoJaExisteException();
        }

        var servico = Servico.Criar(
            id: Guid.NewGuid(),
            nome: nome,
            preco: command.Preco.Value,
            duracaoMin: command.DuracaoMin.Value);

        await _repositorio.AdicionarAsync(servico, command.TraceId, command.UsuarioId, cancellationToken).ConfigureAwait(false);

        return new CriarServicoResponse
        {
            Id = servico.Id,
            Mensagem = "Serviço cadastrado com sucesso.",
            TraceId = command.TraceId,
        };
    }
}
