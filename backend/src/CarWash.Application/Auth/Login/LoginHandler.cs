using CarWash.Application.Abstractions;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Auth.Abstractions;
using CarWash.Application.Common.Exceptions;
using CarWash.Application.Common.Security;
using CarWash.Application.Usuarios.Persistence;
using CarWash.Application.Usuarios.Preferencias.Persistence;
using Microsoft.Extensions.Logging;

namespace CarWash.Application.Auth.Login;

/// <summary>
/// Use case de autenticação. Ordem crítica:
/// <list type="number">
///   <item>Normaliza e-mail (trim + lower).</item>
///   <item>Busca usuário; se inexistente usa <see cref="DummyPasswordHash"/> no
///   <c>Verify</c> para nivelar latência (anti-enumeration).</item>
///   <item>Se o usuário existe e está bloqueado (<see cref="Domain.Entities.Usuario.EstaBloqueado"/>),
///   lança <see cref="UsuarioBloqueadoException"/> imediatamente (RF001 — lockout).</item>
///   <item>Verifica credencial — se falhar para usuário existente, incrementa contador
///   de falhas; ao atingir <see cref="LimiteTentativasInvalidas"/> aplica bloqueio de
///   <see cref="DuracaoBloqueio"/> e lança <see cref="UsuarioBloqueadoException"/>.
///   Caso contrário, lança <see cref="InvalidCredentialsException"/> (mensagem unificada).</item>
///   <item>Só depois verifica <c>Ativo</c>. Inativo + senha correta vira
///   <see cref="UsuarioInativoException"/>.</item>
///   <item>Em sucesso: zera contador (<see cref="Domain.Entities.Usuario.RegistrarLoginBemSucedido"/>),
///   faz rehash silencioso se parâmetros mudaram e persiste uma única vez.</item>
///   <item>Emite access JWT via <see cref="IAccessTokenService"/> + refresh via
///   <see cref="IRefreshTokenService"/> (persiste <c>UsuarioSessao</c>) e audita sucesso.</item>
/// </list>
/// Nunca registra senha, hash ou token em log. E-mail é registrado apenas
/// mascarado (<see cref="EmailMasker.Mask(string?)"/>).
/// <para>
/// Nota (card-134): trecho de lockout temporariamente comentado. Veja blocos
/// marcados com <c>BEGIN(lockout-disabled card-134)</c> / <c>END</c> para reativação.
/// </para>
/// </summary>
public sealed class LoginHandler : ICommandHandler<LoginCommand, LoginResultado>
{
    public const string EventoSucesso = "UsuarioLoginSucesso";
    public const string EventoFalha = "UsuarioLoginFalha";
    public const string EventoUsuarioBloqueado = "UsuarioContaBloqueada";
    public const string EntidadeAuditoria = "Usuario";
    public const string MotivoCredencialInvalida = "CredencialInvalida";
    public const string MotivoUsuarioInativo = "UsuarioInativo";
    public const string MotivoUsuarioBloqueado = "UsuarioBloqueado";

    /// <summary>
    /// RF001 / CA011 — limite de falhas consecutivas a partir do qual o usuário é
    /// bloqueado temporariamente. A semântica esperada (QA POST_login T9) é:
    /// tentativas 1..3 retornam 401 (`InvalidCredentialsException`); na 4ª falha
    /// consecutiva o handler aplica o lockout e responde 403. Por isso o limite
    /// efetivo é 4 — só ao atingir 4 falhas o bloqueio é ativado.
    /// </summary>
    public const int LimiteTentativasInvalidas = 4;

    /// <summary>RF001: duração do lockout temporário aplicado ao atingir o limite.</summary>
    public static readonly TimeSpan DuracaoBloqueio = TimeSpan.FromMinutes(15);

    private readonly IUsuarioRepository _repositorio;
    private readonly IPasswordHasher _hasher;
    private readonly IAccessTokenService _accessTokens;
    private readonly IRefreshTokenService _refreshTokens;
    private readonly IAuditLogger _auditoria;
    private readonly ICurrentRequestContext _contexto;
    private readonly DummyPasswordHash _dummy;
    private readonly ILogger<LoginHandler> _log;
    private readonly IUsuarioPreferenciaRepository _preferencias;

    public LoginHandler(
        IUsuarioRepository repositorio,
        IPasswordHasher hasher,
        IAccessTokenService accessTokens,
        IRefreshTokenService refreshTokens,
        IAuditLogger auditoria,
        ICurrentRequestContext contexto,
        DummyPasswordHash dummy,
        IUsuarioPreferenciaRepository preferencias,
        ILogger<LoginHandler> log)
    {
        _repositorio = repositorio;
        _hasher = hasher;
        _accessTokens = accessTokens;
        _refreshTokens = refreshTokens;
        _auditoria = auditoria;
        _contexto = contexto;
        _dummy = dummy;
        _log = log;
        _preferencias = preferencias;
    }

    /// <inheritdoc/>
    public async Task<LoginResultado> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        string emailNormalizado = (command.Email ?? string.Empty).Trim().ToLowerInvariant();
        string emailMascarado = EmailMasker.Mask(emailNormalizado);

        var usuario = await _repositorio.ObterPorEmailAsync(emailNormalizado, cancellationToken)
                                         .ConfigureAwait(false);

        // Sempre executa o Verify (com hash real ou dummy) para nivelar latência
        // — anti-enumeration. O resultado é usado nas decisões abaixo.
        string hashParaVerificar = usuario?.SenhaHash ?? _dummy.Value;
        bool senhaConfere = _hasher.Verify(command.Senha ?? string.Empty, hashParaVerificar);

#pragma warning disable S1481 // Preservado para reativação do lockout (card-134).
        var agora = DateTime.UtcNow;
#pragma warning restore S1481

        // Bloqueio ativo tem precedência sobre tudo (mesmo senha correta não destrava antes do tempo).
        // Lockout temporariamente desativado — usuário pode tentar N vezes sem bloqueio.
#pragma warning disable S125, SA1512 // Bloco preservado para reativação do lockout (card-134).
        // BEGIN(lockout-disabled card-134): reativar via card de retomada
        // if (usuario is not null && usuario.EstaBloqueado(agora))
        // {
        //     _log.LogWarning(
        //         "Falha de login (usuário bloqueado). UsuarioId={UsuarioId}, Email={Email}, BloqueadoAte={BloqueadoAte:o}",
        //         usuario.Id,
        //         emailMascarado,
        //         usuario.BloqueadoAte!.Value);
        //
        //     _contexto.DefinirEvento(EventoFalha);
        //     await _auditoria.LogAsync(
        //         evento: EventoFalha,
        //         entidade: EntidadeAuditoria,
        //         entidadeId: usuario.Id,
        //         dados: new { Motivo = MotivoUsuarioBloqueado },
        //         cancellationToken: cancellationToken).ConfigureAwait(false);
        //
        //     throw new UsuarioBloqueadoException(usuario.BloqueadoAte!.Value);
        // }
        // END(lockout-disabled card-134)
#pragma warning restore S125, SA1512

        if (usuario is null || !senhaConfere)
        {
            // Senha errada para usuário existente: incrementa contador e — se cruzar o
            // limite — aplica lockout antes de lançar.
            // Lockout temporariamente desativado — usuário pode tentar N vezes sem bloqueio.
#pragma warning disable S125, SA1512 // Bloco preservado para reativação do lockout (card-134).
            // BEGIN(lockout-disabled card-134): reativar via card de retomada
            // if (usuario is not null)
            // {
            //     usuario.RegistrarFalhaDeLogin(agora, LimiteTentativasInvalidas, DuracaoBloqueio);
            //     await _repositorio.SalvarAsync(cancellationToken).ConfigureAwait(false);
            //
            //     if (usuario.EstaBloqueado(agora))
            //     {
            //         _log.LogWarning(
            //             "Conta bloqueada por excesso de tentativas inválidas. UsuarioId={UsuarioId}, Email={Email}, BloqueadoAte={BloqueadoAte:o}",
            //             usuario.Id,
            //             emailMascarado,
            //             usuario.BloqueadoAte!.Value);
            //
            //         _contexto.DefinirEvento(EventoUsuarioBloqueado);
            //         await _auditoria.LogAsync(
            //             evento: EventoUsuarioBloqueado,
            //             entidade: EntidadeAuditoria,
            //             entidadeId: usuario.Id,
            //             dados: new
            //             {
            //                 Motivo = MotivoUsuarioBloqueado,
            //                 usuario.TentativasInvalidas,
            //                 BloqueadoAte = usuario.BloqueadoAte!.Value,
            //             },
            //             cancellationToken: cancellationToken).ConfigureAwait(false);
            //
            //         throw new UsuarioBloqueadoException(usuario.BloqueadoAte!.Value);
            //     }
            // }
            // END(lockout-disabled card-134)
#pragma warning restore S125, SA1512

            _log.LogWarning(
                "Falha de login (credencial inválida). Email={Email}",
                emailMascarado);

            _contexto.DefinirEvento(EventoFalha);
            await _auditoria.LogAsync(
                evento: EventoFalha,
                entidade: EntidadeAuditoria,
                entidadeId: usuario?.Id,
                dados: new { Motivo = MotivoCredencialInvalida },
                cancellationToken: cancellationToken).ConfigureAwait(false);

            throw new InvalidCredentialsException();
        }

        if (!usuario.Ativo)
        {
            _log.LogWarning(
                "Falha de login (usuário inativo). UsuarioId={UsuarioId}, Email={Email}",
                usuario.Id,
                emailMascarado);

            _contexto.DefinirEvento(EventoFalha);
            await _auditoria.LogAsync(
                evento: EventoFalha,
                entidade: EntidadeAuditoria,
                entidadeId: usuario.Id,
                dados: new { Motivo = MotivoUsuarioInativo },
                cancellationToken: cancellationToken).ConfigureAwait(false);

            throw new UsuarioInativoException();
        }

        // Sucesso: zera contador, libera bloqueio (idempotente) e rehash silencioso se preciso.
        // Tudo persistido em uma única SalvarAsync.
        usuario.RegistrarLoginBemSucedido();
        bool precisaRehash = _hasher.NeedsRehash(usuario.SenhaHash);
        if (precisaRehash)
        {
            usuario.TrocarSenha(_hasher.Hash(command.Senha!));
        }

        await _repositorio.SalvarAsync(cancellationToken).ConfigureAwait(false);

        var (accessToken, accessExpiresAt) = _accessTokens.Emitir(usuario);
        var refresh = await _refreshTokens.EmitirAsync(usuario, cancellationToken).ConfigureAwait(false);

        _contexto.DefinirEvento(EventoSucesso);
        await _auditoria.LogAsync(
            evento: EventoSucesso,
            entidade: EntidadeAuditoria,
            entidadeId: usuario.Id,
            dados: new { SessaoId = refresh.SessaoId },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        _log.LogInformation(
            "Login bem-sucedido. UsuarioId={UsuarioId}, Email={Email}, SessaoId={SessaoId}",
            usuario.Id,
            emailMascarado,
            refresh.SessaoId);

        var preferencia = await _preferencias
        .ObterPorUsuarioIdAsync(usuario.Id, cancellationToken)
        .ConfigureAwait(false);

        string theme = preferencia?.TemaRaw ?? "light";

        return new LoginResultado(
            AccessToken: accessToken,
            AccessExpiresAt: accessExpiresAt,
            RefreshToken: refresh.RefreshToken,
            RefreshExpiresAt: refresh.ExpiraEm,
            Usuario: new LoginResultado.UsuarioLogado(
                usuario.Id,
                usuario.Nome,
                usuario.EmailValor,
                usuario.Perfil,
                theme));
    }
}
