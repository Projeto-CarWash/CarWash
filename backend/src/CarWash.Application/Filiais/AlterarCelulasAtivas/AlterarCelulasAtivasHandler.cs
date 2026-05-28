using CarWash.Application.Abstractions;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Filiais.Common;
using CarWash.Application.Filiais.Persistence;
using Microsoft.Extensions.Logging;

namespace CarWash.Application.Filiais.AlterarCelulasAtivas;

/// <summary>
/// Use case de alteração da quantidade de células ativas de uma filial (RF018).
/// Idempotente: se o valor atual já bater com o solicitado, devolve estado
/// sem salvar nem auditar (mesmo padrão de
/// <see cref="Usuarios.AlterarStatus.AlterarStatusUsuarioHandler"/>).
/// </summary>
public sealed class AlterarCelulasAtivasHandler
    : ICommandHandler<AlterarCelulasAtivasCommand, FilialResponse>
{
    public const string EventoAuditoria = "FilialCelulasAlteradas";
    public const string EntidadeAuditoria = "Filial";
    public const string MensagemNaoEncontrado = "Filial não encontrada.";

    private readonly IFilialRepository _repo;
    private readonly IAuditLogger _audit;
    private readonly ICurrentRequestContext _ctx;
    private readonly ILogger<AlterarCelulasAtivasHandler> _log;

    public AlterarCelulasAtivasHandler(
        IFilialRepository repo,
        IAuditLogger audit,
        ICurrentRequestContext ctx,
        ILogger<AlterarCelulasAtivasHandler> log)
    {
        _repo = repo;
        _audit = audit;
        _ctx = ctx;
        _log = log;
    }

    public async Task<FilialResponse> HandleAsync(
        AlterarCelulasAtivasCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Validator garante NotNull antes de chegar aqui — `.Value` é seguro.
        var valorSolicitado = command.CelulasAtivas!.Value;

        // ObterPorIdAsync (development) retorna a entidade RASTREADA — permite
        // mutar (AjustarCelulas) e persistir via SalvarAsync na mesma unidade
        // de trabalho.
        var filial = await _repo.ObterPorIdAsync(command.FilialId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(MensagemNaoEncontrado);

        var valorAnterior = filial.CelulasAtivas;

        if (valorAnterior == valorSolicitado)
        {
            // No-op: nenhum SaveChanges, nenhum audit, nenhum AtualizadoEm.
            _log.LogInformation(
                "Células ativas já é {Valor} para filial {FilialId} — no-op (sem save, sem audit).",
                valorAnterior,
                filial.Id);
            return FilialResponse.FromEntity(filial);
        }

        // O mutator do domínio reforça a faixa 1..100 (DomainException → 400 via
        // middleware), mas o validator já cobriu antes — defesa em profundidade.
        filial.AjustarCelulas(valorSolicitado);

        // Definir o evento ANTES do SaveChanges permite que o AuditLogInterceptor
        // capture o diff (celulas_ativas: before/after) na mesma transação.
        _ctx.DefinirEvento(EventoAuditoria);

        await _repo.SalvarAsync(cancellationToken).ConfigureAwait(false);

        // Auditoria explícita com valorAnterior/valorNovo — atende ao critério
        // do card "auditoria com valorAnterior/valorNovo".
        await _audit.LogAsync(
            evento: EventoAuditoria,
            entidade: EntidadeAuditoria,
            entidadeId: filial.Id,
            dados: new
            {
                valorAnterior,
                valorNovo = filial.CelulasAtivas,
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        _log.LogInformation(
            "Células ajustadas. FilialId={FilialId}, De={De}, Para={Para}",
            filial.Id,
            valorAnterior,
            filial.CelulasAtivas);

        return FilialResponse.FromEntity(filial);
    }
}
