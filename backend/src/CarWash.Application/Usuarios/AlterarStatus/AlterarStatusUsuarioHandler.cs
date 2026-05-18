using CarWash.Application.Abstractions;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Usuarios.Persistence;
using CarWash.Domain.Entities;
using CarWash.Domain.Enums;
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

    public const string MensagemAutoDesativacao =
        "Você não pode desativar a própria conta de usuário.";

    public const string SlugAutoDesativacao = "auto-desativacao-bloqueada";

    public const string MensagemUltimoAdminAtivo =
        "Não é possível desativar o último administrador ativo do sistema.";

    public const string SlugUltimoAdminAtivo = "ultimo-admin-ativo";

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

        // Validator garante NotNull antes de chegar aqui. `.Value` é seguro.
        var ativoDesejado = command.Ativo!.Value;

        _log.LogInformation(
            "Solicitação de alteração de status. UsuarioId={UsuarioId}, AtivoSolicitado={AtivoSolicitado}",
            command.UsuarioId,
            ativoDesejado);

        var usuario = await _repositorio.ObterPorIdRastreadoAsync(command.UsuarioId, cancellationToken)
                            .ConfigureAwait(false)
                          ?? throw new NotFoundException(MensagemNaoEncontrado);

        if (usuario.Ativo == ativoDesejado)
        {
            _log.LogInformation(
                "Status já é {AtivoAtual} para usuário {UsuarioId} — no-op (sem save, sem audit).",
                usuario.Ativo,
                usuario.Id);
            return ToResponse(usuario);
        }

        var estadoAnterior = usuario.Ativo;

        // BUG-U009 (auto-desativação / último admin): só vale quando o alvo está
        // sendo INATIVADO. Reativação não tem risco. Avalia em duas frentes
        // complementares — auto-desativação primeiro (mensagem mais útil ao caller),
        // depois "último admin ativo".
        if (!ativoDesejado)
        {
            GarantirNaoEhAutoDesativacao(usuario);
            await GarantirNaoEhUltimoAdminAtivoAsync(usuario, cancellationToken).ConfigureAwait(false);
        }

        if (ativoDesejado)
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

    private void GarantirNaoEhAutoDesativacao(Usuario alvo)
    {
        var usuarioLogadoId = _contexto.UsuarioId;
        if (usuarioLogadoId.HasValue && usuarioLogadoId.Value == alvo.Id)
        {
            _log.LogWarning(
                "Tentativa de auto-desativação bloqueada. UsuarioId={UsuarioId}",
                alvo.Id);
            throw new ConflictException(MensagemAutoDesativacao, SlugAutoDesativacao);
        }
    }

    private async Task GarantirNaoEhUltimoAdminAtivoAsync(Usuario alvo, CancellationToken cancellationToken)
    {
        if (alvo.Perfil != PerfilUsuario.Admin)
        {
            return;
        }

        var totalAdminsAtivos = await _repositorio.ContarAdminsAtivosAsync(cancellationToken).ConfigureAwait(false);
        if (totalAdminsAtivos <= 1)
        {
            _log.LogWarning(
                "Tentativa de desativar o último admin ativo bloqueada. UsuarioId={UsuarioId}, AdminsAtivos={AdminsAtivos}",
                alvo.Id,
                totalAdminsAtivos);
            throw new ConflictException(MensagemUltimoAdminAtivo, SlugUltimoAdminAtivo);
        }
    }
}
