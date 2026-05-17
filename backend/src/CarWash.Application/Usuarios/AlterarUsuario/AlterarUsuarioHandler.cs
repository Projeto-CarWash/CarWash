using CarWash.Application.Abstractions;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Usuarios.Common;
using CarWash.Application.Usuarios.Persistence;
using CarWash.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CarWash.Application.Usuarios.AlterarUsuario;

/// <summary>
/// Atualiza nome, e-mail e perfil de um usuário existente. Falha com:
/// <list type="bullet">
///   <item><see cref="NotFoundException"/> 404 se o id não existe.</item>
///   <item><see cref="ConflictException"/> 409 se o e-mail já está em uso por outro usuário.</item>
/// </list>
/// </summary>
public sealed class AlterarUsuarioHandler : ICommandHandler<AlterarUsuarioCommand, UsuarioResponse>
{
    public const string EventoSucesso = "UsuarioAlterado";
    public const string EntidadeAuditoria = "Usuario";

    private readonly IUsuarioRepository _repo;
    private readonly IAuditLogger _audit;
    private readonly ICurrentRequestContext _ctx;
    private readonly ILogger<AlterarUsuarioHandler> _log;

    public AlterarUsuarioHandler(
        IUsuarioRepository repo,
        IAuditLogger audit,
        ICurrentRequestContext ctx,
        ILogger<AlterarUsuarioHandler> log)
    {
        _repo = repo;
        _audit = audit;
        _ctx = ctx;
        _log = log;
    }

    public async Task<UsuarioResponse> HandleAsync(AlterarUsuarioCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var usuario = await _repo.ObterPorIdRastreadoAsync(command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Usuário não encontrado.");

        var emailNormalizado = command.Email.Trim().ToLowerInvariant();

        if (!string.Equals(usuario.EmailValor, emailNormalizado, StringComparison.Ordinal))
        {
            var emailEmUso = await _repo.ExisteComEmailAsync(emailNormalizado, cancellationToken).ConfigureAwait(false);
            if (emailEmUso)
            {
                throw new ConflictException(
                    "Já existe outro usuário cadastrado com este e-mail.",
                    "usuario-email-duplicado");
            }
        }

        usuario.AlterarDados(command.Nome.Trim(), new Email(emailNormalizado), command.Perfil);

        await _repo.SalvarAsync(cancellationToken).ConfigureAwait(false);

        _ctx.DefinirEvento(EventoSucesso);
        await _audit.LogAsync(
            evento: EventoSucesso,
            entidade: EntidadeAuditoria,
            entidadeId: usuario.Id,
            dados: null,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        _log.LogInformation("Usuário alterado. UsuarioId={UsuarioId}", usuario.Id);

        return UsuarioResponse.FromEntity(usuario);
    }
}
