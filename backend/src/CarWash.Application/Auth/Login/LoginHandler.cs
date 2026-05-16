using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CarWash.Application.Abstractions;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Auth.Abstractions;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Common.Security;
using CarWash.Application.Usuarios.Persistence;
using Microsoft.Extensions.Logging;

namespace CarWash.Application.Auth.Login;

/// <summary>
/// Use case de autenticação. Ordem crítica:
/// <list type="number">
///   <item>Normaliza e-mail (trim + lower).</item>
///   <item>Busca usuário; se inexistente usa <see cref="DummyPasswordHash"/> no
///   <c>Verify</c> para nivelar latência (anti-enumeration).</item>
///   <item>Verifica credencial — se falhar, lança <see cref="InvalidCredentialsException"/>
///   (mensagem unificada).</item>
///   <item>Só depois verifica <c>Ativo</c>. Inativo + senha correta vira
///   <see cref="UsuarioInativoException"/>.</item>
///   <item>Rehash silencioso se parâmetros mudaram.</item>
///   <item>Emite token opaco via <see cref="IAuthTokenService"/> e audita sucesso.</item>
/// </list>
/// Nunca registra senha, hash ou token em log. E-mail é registrado apenas como
/// hash truncado (<c>emailHash</c>).
/// </summary>
public sealed class LoginHandler : ICommandHandler<LoginCommand, LoginResponse>
{
    public const string EventoSucesso = "UsuarioLoginSucesso";
    public const string EventoFalha = "UsuarioLoginFalha";
    public const string EntidadeAuditoria = "Usuario";
    public const string MotivoCredencialInvalida = "CredencialInvalida";
    public const string MotivoUsuarioInativo = "UsuarioInativo";

    private readonly IUsuarioRepository _repositorio;
    private readonly IPasswordHasher _hasher;
    private readonly IAuthTokenService _tokens;
    private readonly IAuditLogger _auditoria;
    private readonly ICurrentRequestContext _contexto;
    private readonly DummyPasswordHash _dummy;
    private readonly ILogger<LoginHandler> _log;

    public LoginHandler(
        IUsuarioRepository repositorio,
        IPasswordHasher hasher,
        IAuthTokenService tokens,
        IAuditLogger auditoria,
        ICurrentRequestContext contexto,
        DummyPasswordHash dummy,
        ILogger<LoginHandler> log)
    {
        _repositorio = repositorio;
        _hasher = hasher;
        _tokens = tokens;
        _auditoria = auditoria;
        _contexto = contexto;
        _dummy = dummy;
        _log = log;
    }

    public async Task<LoginResponse> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var emailNormalizado = (command.Email ?? string.Empty).Trim().ToLowerInvariant();
        var emailHash = HashTruncado(emailNormalizado);

        var usuario = await _repositorio.ObterPorEmailAsync(emailNormalizado, cancellationToken)
                                         .ConfigureAwait(false);

        var hashParaVerificar = usuario?.SenhaHash ?? _dummy.Value;
        var senhaConfere = _hasher.Verify(command.Senha ?? string.Empty, hashParaVerificar);

        if (usuario is null || !senhaConfere)
        {
            _log.LogWarning(
                "Falha de login (credencial inválida). EmailHash={EmailHash}",
                emailHash);

            _contexto.DefinirEvento(EventoFalha);
            await _auditoria.LogAsync(
                evento: EventoFalha,
                entidade: EntidadeAuditoria,
                entidadeId: null,
                dados: new { Motivo = MotivoCredencialInvalida },
                cancellationToken: cancellationToken).ConfigureAwait(false);

            throw new InvalidCredentialsException();
        }

        if (!usuario.Ativo)
        {
            _log.LogWarning(
                "Falha de login (usuário inativo). UsuarioId={UsuarioId}, EmailHash={EmailHash}",
                usuario.Id,
                emailHash);

            _contexto.DefinirEvento(EventoFalha);
            await _auditoria.LogAsync(
                evento: EventoFalha,
                entidade: EntidadeAuditoria,
                entidadeId: usuario.Id,
                dados: new { Motivo = MotivoUsuarioInativo },
                cancellationToken: cancellationToken).ConfigureAwait(false);

            throw new UsuarioInativoException();
        }

        if (_hasher.NeedsRehash(usuario.SenhaHash))
        {
            usuario.TrocarSenha(_hasher.Hash(command.Senha!));
            await _repositorio.SalvarAsync(cancellationToken).ConfigureAwait(false);
        }

        var (token, expiresAt) = await _tokens.EmitirAsync(usuario, cancellationToken).ConfigureAwait(false);

        _contexto.DefinirEvento(EventoSucesso);
        await _auditoria.LogAsync(
            evento: EventoSucesso,
            entidade: EntidadeAuditoria,
            entidadeId: usuario.Id,
            dados: null,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        _log.LogInformation(
            "Login bem-sucedido. UsuarioId={UsuarioId}, EmailHash={EmailHash}",
            usuario.Id,
            emailHash);

        return new LoginResponse(
            AccessToken: token,
            ExpiresAt: expiresAt,
            Usuario: new LoginResponse.UsuarioLogado(
                usuario.Id,
                usuario.Nome,
                usuario.EmailValor,
                usuario.Perfil));
    }

    private static string HashTruncado(string valor)
    {
        if (string.IsNullOrEmpty(valor))
        {
            return string.Empty;
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(valor));
        var sb = new StringBuilder(12);
        for (var i = 0; i < 6; i++)
        {
            sb.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }
}
