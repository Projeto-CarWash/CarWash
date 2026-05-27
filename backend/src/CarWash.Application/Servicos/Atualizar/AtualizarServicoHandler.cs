using System.Text.Json;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Common;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Servicos.Common;
using CarWash.Application.Servicos.Persistence;
using DomainAuditLog = CarWash.Domain.Entities.AuditLog;

namespace CarWash.Application.Servicos.Atualizar;

/// <summary>
/// Use case de atualização de serviço. <c>PATCH /api/v1/servicos/{id}</c>.
/// </summary>
public sealed class AtualizarServicoHandler : ICommandHandler<AtualizarServicoCommand, ServicoResponse>
{
    private readonly IServicoRepository _repositorio;

    public AtualizarServicoHandler(IServicoRepository repositorio)
    {
        _repositorio = repositorio;
    }

    /// <inheritdoc/>
    public async Task<ServicoResponse> HandleAsync(AtualizarServicoCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Defesa em profundidade: validator já exige NotNull em Preco/DuracaoMin.
        // Bloqueio de fallback para nunca cair em InvalidOperationException no AtualizarDados.
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

        var servico = await _repositorio.ObterPorIdAsync(command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Serviço não encontrado.");

        string nome = InputNormalizer.SanitizeTextOrNull(command.Nome)!;

        // GAP-CW-SRV-PATCH-NOME: nome deve continuar único entre serviços,
        // ignorando o próprio serviço (permite manter o mesmo valor).
        if (await _repositorio.ExisteNomeAsync(nome, ignoreServicoId: command.Id, cancellationToken).ConfigureAwait(false))
        {
            throw new NomeServicoJaExisteException();
        }

        servico.AtualizarDados(
            nome: nome,
            preco: command.Preco.Value,
            duracaoMin: command.DuracaoMin.Value);

        await _repositorio.SalvarAsync(cancellationToken).ConfigureAwait(false);

        await _repositorio.RegistrarAuditoriaAsync(
            evento: "SERVICO_ATUALIZADO",
            entidadeId: servico.Id,
            correlationId: command.TraceId,
            usuarioId: command.UsuarioId,
            dados: JsonSerializer.Serialize(new
            {
                servico.Id,
                servico.Nome,
                servico.Preco,
                servico.DuracaoMin,
            }),
            cancellationToken).ConfigureAwait(false);

        return ServicoResponse.FromEntity(servico);
    }
}
