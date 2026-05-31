using CarWash.Application.Abstractions;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Usuarios.Common;
using CarWash.Application.Usuarios.Persistence;
using CarWash.Domain.Common;
using CarWash.Domain.Entities;
using CarWash.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CarWash.Application.Usuarios.CriarUsuario;

/// <summary>
/// Use case de cadastro de usuário (CA1-CA7, CA11). Defesa em duas camadas para
/// unicidade de e-mail: pré-check + UK <c>uk_usuarios_email</c>. Senha sempre
/// persistida como hash Argon2id; nunca em logs.
/// <para>
/// O tratamento da race condition vive no <see cref="IUsuarioRepository"/>
/// (Infrastructure), que traduz <see cref="Microsoft.EntityFrameworkCore.DbUpdateException"/>
/// em <see cref="EmailJaExisteException"/> — mantendo a Application livre de
/// dependências do EF/Npgsql.
/// </para>
/// </summary>
public sealed class CriarUsuarioHandler : ICommandHandler<CriarUsuarioCommand, UsuarioResponse>
{
    public const string EventoAuditoria = "UsuarioCadastrado";

    /// <summary>
    /// Mantida para retrocompatibilidade dos testes existentes do contrato HTTP.
    /// </summary>
    public const string MensagemEmailDuplicado = EmailJaExisteException.MensagemPadrao;

    /// <summary>
    /// Mantida para retrocompatibilidade dos testes existentes do contrato HTTP.
    /// </summary>
    public const string SlugEmailDuplicado = EmailJaExisteException.SlugPadrao;

    private readonly IUsuarioRepository _repositorio;
    private readonly IPasswordHasher _hasher;
    private readonly ICurrentRequestContext _contexto;
    private readonly ILogger<CriarUsuarioHandler> _log;

    public CriarUsuarioHandler(
        IUsuarioRepository repositorio,
        IPasswordHasher hasher,
        ICurrentRequestContext contexto,
        ILogger<CriarUsuarioHandler> log)
    {
        _repositorio = repositorio;
        _hasher = hasher;
        _contexto = contexto;
        _log = log;
    }

    /// <inheritdoc/>
    public async Task<UsuarioResponse> HandleAsync(CriarUsuarioCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        _contexto.DefinirEvento(EventoAuditoria);

        // Value object normaliza para lowercase + valida formato.
        Email email;
        try
        {
            email = new Email(command.Email);
        }
        catch (DomainException ex)
        {
            throw new ValidationException(
                CriarUsuarioCommandValidator.MensagemPayloadInvalido,
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["email"] = [ex.Message],
                });
        }

        // Camada 1 — pré-check (mensagem amigável antes de bater no banco).
        if (await _repositorio.ExisteComEmailAsync(email.Valor, cancellationToken).ConfigureAwait(false))
        {
            _log.LogWarning("Tentativa de cadastro com e-mail já existente.");
            throw new EmailJaExisteException();
        }

        string senhaHash = _hasher.Hash(command.Senha);

        // Perfil é nullable no command para diferenciar "ausente" de "Admin (0)".
        // O validator garante NotNull antes deste ponto — `.Value` é seguro.
        var usuario = Usuario.Criar(
            id: Guid.NewGuid(),
            nome: command.Nome.Trim(),
            email: email,
            senhaHash: senhaHash,
            perfil: command.Perfil!.Value);

        await _repositorio.AdicionarAsync(usuario, cancellationToken).ConfigureAwait(false);

        // Camada 2 — UK do banco protege contra race condition. O repositório
        // intercepta DbUpdateException → EmailJaExisteException antes de
        // borbulhar para cá, mantendo a Application livre de EF/Npgsql.
        await _repositorio.SalvarAsync(cancellationToken).ConfigureAwait(false);

        _log.LogInformation("Usuário {UsuarioId} cadastrado.", usuario.Id);

        return UsuarioResponse.FromEntity(usuario);
    }
}
