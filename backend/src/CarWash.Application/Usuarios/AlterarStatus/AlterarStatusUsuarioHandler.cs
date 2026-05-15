using CarWash.Application.Abstractions;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Usuarios.Persistence;
using CarWash.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CarWash.Application.Usuarios.AlterarStatus;

/// <summary>
/// Use case de ativar/inativar usuário. Idempotente: no-op se já está no estado
/// solicitado (sem save, sem audit, sem mudança de <c>AtualizadoEm</c>). Quando
/// houve mudança real, emite <c>UsuarioStatusAlterado</c> com <c>{De, Para}</c>.
/// </summary>
public sealed class AlterarStatusUsuarioHandler
    : ICommandHandler<AlterarStatusUsuarioCommand, AlterarStatusUsuarioResponse>
{
    public const string EventoAuditoria = "UsuarioStatusAlterado";
    public const string MensagemNaoEncontrado = "Usuário não encontrado.";

    private readonly IUsuarioRepository _repositorio;
    private readonly ICurrentRequestContext _contexto;
    private readonly ILogger<AlterarStatusUsuarioHandler> _log;

    public AlterarStatusUsuarioHandler(
        IUsuarioRepository repositorio,
        ICurrentRequestContext contexto,
        ILogger<AlterarStatusUsuarioHandler> log)
    {
        _repositorio = repositorio;
        _contexto = contexto;
        _log = log;
    }

    public async Task<AlterarStatusUsuarioResponse> HandleAsync(
        AlterarStatusUsuarioCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        _log.LogInformation(
            "Solicitação de alteração de status. UsuarioId={UsuarioId}, AtivoSolicitado={AtivoSolicitado}",
            command.UsuarioId,
            command.Ativo);

        var usuario = await _repositorio.ObterPorIdRastreadoAsync(command.UsuarioId, cancellationToken)
                            .ConfigureAwait(false)
                          ?? throw new NotFoundException(MensagemNaoEncontrado);

        if (usuario.Ativo == command.Ativo)
        {
            _log.LogInformation(
                "Status já é {AtivoAtual} para usuário {UsuarioId} — no-op (sem save, sem audit).",
                usuario.Ativo,
                usuario.Id);
            return ToResponse(usuario);
        }

        var estadoAnterior = usuario.Ativo;

        if (command.Ativo)
        {
            usuario.Ativar();
        }
        else
        {
            usuario.Inativar();
        }

        _contexto.DefinirEvento(EventoAuditoria);

        await _repositorio.SalvarAsync(cancellationToken).ConfigureAwait(false);

        _log.LogInformation(
            "Status de usuário alterado. UsuarioId={UsuarioId}, De={De}, Para={Para}",
            usuario.Id,
            estadoAnterior,
            usuario.Ativo);

        return ToResponse(usuario);
    }

    private static AlterarStatusUsuarioResponse ToResponse(Usuario usuario) =>
        new(usuario.Id, usuario.Ativo, usuario.AtualizadoEm);
}
