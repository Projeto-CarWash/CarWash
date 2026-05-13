using CarWash.Application.Abstractions;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Usuarios.Common;
using CarWash.Application.Usuarios.Persistence;
using CarWash.Domain.Common;
using CarWash.Domain.Entities;
using CarWash.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CarWash.Application.Usuarios.CriarUsuario;

/// <summary>
/// Use case de cadastro de usuário (CA1-CA7, CA11). Defesa em duas camadas para
/// unicidade de e-mail: pré-check + UK <c>uk_usuarios_email</c>. Senha sempre
/// persistida como hash Argon2id; nunca em logs.
/// </summary>
public sealed class CriarUsuarioHandler : ICommandHandler<CriarUsuarioCommand, UsuarioResponse>
{
    public const string EventoAuditoria = "UsuarioCadastrado";
    public const string MensagemEmailDuplicado = "Já existe usuário cadastrado com este e-mail.";
    public const string SlugEmailDuplicado = "email-already-exists";
    private const string ConstraintEmailUnico = "uk_usuarios_email";
    private const string PostgresUniqueViolationSqlState = "23505";

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
            throw new ConflictException(MensagemEmailDuplicado, SlugEmailDuplicado);
        }

        var senhaHash = _hasher.Hash(command.Senha);

        var usuario = Usuario.Criar(
            id: Guid.NewGuid(),
            nome: command.Nome.Trim(),
            email: email,
            senhaHash: senhaHash,
            perfil: command.Perfil);

        await _repositorio.AdicionarAsync(usuario, cancellationToken).ConfigureAwait(false);

        try
        {
            // Camada 2 — UK do banco protege contra race condition.
            await _repositorio.SalvarAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (IsEmailUniqueViolation(ex))
        {
            _log.LogWarning("Race condition de e-mail duplicado capturada pela UK uk_usuarios_email.");
            throw new ConflictException(MensagemEmailDuplicado, SlugEmailDuplicado, ex);
        }

        _log.LogInformation("Usuário {UsuarioId} cadastrado.", usuario.Id);

        return UsuarioResponse.FromEntity(usuario);
    }

    private static bool IsEmailUniqueViolation(DbUpdateException ex)
    {
        if (ex.InnerException is not PostgresException pg)
        {
            return false;
        }

        return string.Equals(pg.SqlState, PostgresUniqueViolationSqlState, StringComparison.Ordinal)
            && pg.ConstraintName is not null
            && pg.ConstraintName.Contains(ConstraintEmailUnico, StringComparison.OrdinalIgnoreCase);
    }
}
