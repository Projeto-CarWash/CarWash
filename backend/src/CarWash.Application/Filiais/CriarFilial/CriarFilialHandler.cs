using CarWash.Application.Abstractions;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Filiais.Common;
using CarWash.Application.Filiais.Persistence;
using CarWash.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CarWash.Application.Filiais.CriarFilial;

/// <summary>
/// Use case de criação de filial (RF017/RF018). Defesa em duas camadas para
/// unicidade do nome (pré-check + UK <c>uk_filiais_nome</c>). O tratamento da
/// race condition vive no <see cref="IFilialRepository"/> (Infrastructure), que
/// traduz <c>DbUpdateException</c> em <see cref="NomeFilialJaExisteException"/>
/// — mantendo a Application livre de dependências do EF/Npgsql.
/// </summary>
public sealed class CriarFilialHandler : ICommandHandler<CriarFilialCommand, FilialResponse>
{
    public const string EventoAuditoria = "FilialCriada";
    public const string EntidadeAuditoria = "Filial";

    private readonly IFilialRepository _repo;
    private readonly IAuditLogger _audit;
    private readonly ICurrentRequestContext _ctx;
    private readonly ILogger<CriarFilialHandler> _log;

    public CriarFilialHandler(
        IFilialRepository repo,
        IAuditLogger audit,
        ICurrentRequestContext ctx,
        ILogger<CriarFilialHandler> log)
    {
        _repo = repo;
        _audit = audit;
        _ctx = ctx;
        _log = log;
    }

    public async Task<FilialResponse> HandleAsync(CriarFilialCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Definir o evento ANTES do SaveChanges permite que o AuditLogInterceptor
        // capture o INSERT da Filial em audit_logs (diff automático na mesma
        // transação).
        _ctx.DefinirEvento(EventoAuditoria);

        // Validator garante NotNull/Trim/Faixa antes de chegar aqui — `.Value` e
        // `Trim()` são seguros.
        var nomeNormalizado = command.Nome!.Trim();

        // Camada 1 — pré-check (mensagem amigável antes de bater no banco).
        if (await _repo.ExisteComNomeAsync(nomeNormalizado, cancellationToken).ConfigureAwait(false))
        {
            _log.LogWarning("Tentativa de criação de filial com nome já existente.");
            throw new NomeFilialJaExisteException();
        }

        var filial = Filial.Criar(
            id: Guid.NewGuid(),
            nome: nomeNormalizado,
            celulasAtivas: command.CelulasAtivas!.Value,
            timezone: command.Timezone);

        await _repo.AdicionarAsync(filial, cancellationToken).ConfigureAwait(false);

        // Camada 2 — UK do banco protege contra race condition. O repositório
        // intercepta DbUpdateException → NomeFilialJaExisteException antes de
        // borbulhar, mantendo a Application livre de EF/Npgsql.
        await _repo.SalvarAsync(cancellationToken).ConfigureAwait(false);

        // Auditoria explícita (além do snapshot do interceptor) — atende ao
        // critério do card "valorAnterior=null / valorNovo" do evento.
        await _audit.LogAsync(
            evento: EventoAuditoria,
            entidade: EntidadeAuditoria,
            entidadeId: filial.Id,
            dados: new
            {
                filial.Nome,
                filial.CelulasAtivas,
                filial.Timezone,
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        _log.LogInformation(
            "Filial {FilialId} criada com {CelulasAtivas} células.",
            filial.Id,
            filial.CelulasAtivas);

        return FilialResponse.FromEntity(filial);
    }
}
