using System.Security.Cryptography;
using CarWash.Application.Auth.Abstractions;
using CarWash.Domain.Entities;

namespace CarWash.Infrastructure.Auth;

/// <summary>
/// Emite tokens opacos (Base64Url de 32 bytes aleatórios) com expiração de 8h —
/// implementação MVP do <see cref="IAuthTokenService"/>. Não persiste sessão no
/// momento; quando <c>UsuarioSessao</c> for integrada ao fluxo de refresh/logout
/// (P05), basta gravar o SHA-256 do token bruto via <c>ITokenHasher</c>.
/// </summary>
public sealed class OpaqueAuthTokenService : IAuthTokenService
{
    public const int TamanhoBytes = 32;
    public static readonly TimeSpan Validade = TimeSpan.FromHours(8);

    public Task<(string Token, DateTime ExpiresAt)> EmitirAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        cancellationToken.ThrowIfCancellationRequested();

        var bytes = RandomNumberGenerator.GetBytes(TamanhoBytes);
        var token = Base64UrlEncode(bytes);
        var expiresAt = DateTime.UtcNow.Add(Validade);

        // TODO(P05): persistir UsuarioSessao com SHA-256(token) quando o fluxo de
        // refresh/logout entrar em escopo. Hoje o token é stateless do lado servidor.
        return Task.FromResult((token, expiresAt));
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> bytes)
    {
        var b64 = Convert.ToBase64String(bytes);
        return b64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
