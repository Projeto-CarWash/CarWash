using CarWash.Application.Abstractions;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Filiais.Common;
using CarWash.Application.Filiais.Persistence;
using Microsoft.Extensions.Logging;

namespace CarWash.Application.Filiais.AlterarStatus;

/// <summary>
/// Use case de ativação/inativação de filial (RF017). Filial inativa não é
/// aceita em novos agendamentos (RF019/RN010 — 409 <c>filial-inativa</c>).
/// Idempotente: se o status atual já bater com o solicitado, devolve estado
/// sem salvar nem auditar (mesmo padrão de
/// <see cref="AlterarCelulasAtivas.AlterarCelulasAtivasHandler"/>).
/// </summary>
public sealed class AlterarStatusFilialHandler
    : ICommandHandler<AlterarStatusFilialCommand, FilialResponse>
{
    public const string EventoAuditoria = "FilialStatusAlterado";
    public const string EntidadeAuditoria = "Filial";
    public const string MensagemNaoEncontrado = "Filial não encontrada.";

    private readonly IFilialRepository _repo;
    private readonly IAuditLogger _audit;
    private readonly ICurrentRequestContext _ctx;
    private readonly ILogger<AlterarStatusFilialHandler> _log;

    public AlterarStatusFilialHandler(
        IFilialRepository repo,
        IAuditLogger audit,
        ICurrentRequestContext ctx,
        ILogger<AlterarStatusFilialHandler> log)
    {
        _repo = repo;
        _audit = audit;
        _ctx = ctx;
        _log = log;
    }

    public async Task<FilialResponse> HandleAsync(
        AlterarStatusFilialCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Validator garante NotNull antes de chegar aqui — `.Value` é seguro.
        bool statusSolicitado = command.Ativo!.Value;

        var filial = await _repo.ObterPorIdAsync(command.FilialId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(MensagemNaoEncontrado);

        bool statusAnterior = filial.Ativa;

        if (statusAnterior == statusSolicitado)
        {
            // No-op: nenhum SaveChanges, nenhum audit, nenhum AtualizadoEm.
            _log.LogInformation(
                "Status já é {Status} para filial {FilialId} — no-op (sem save, sem audit).",
                statusAnterior,
                filial.Id);
            return FilialResponse.FromEntity(filial);
        }

        if (statusSolicitado)
        {
            filial.Ativar();
        }
        else
        {
            filial.Inativar();
        }

        // Definir o evento ANTES do SaveChanges permite que o AuditLogInterceptor
        // capture o diff (ativa: before/after) na mesma transação.
        _ctx.DefinirEvento(EventoAuditoria);

        await _repo.SalvarAsync(cancellationToken).ConfigureAwait(false);

        await _audit.LogAsync(
            evento: EventoAuditoria,
            entidade: EntidadeAuditoria,
            entidadeId: filial.Id,
            dados: new
            {
                valorAnterior = statusAnterior,
                valorNovo = filial.Ativa,
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        _log.LogInformation(
            "Status da filial alterado. FilialId={FilialId}, De={De}, Para={Para}",
            filial.Id,
            statusAnterior,
            filial.Ativa);

        return FilialResponse.FromEntity(filial);
    }
}
