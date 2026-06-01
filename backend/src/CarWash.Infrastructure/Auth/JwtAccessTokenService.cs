using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CarWash.Application.Auth.Abstractions;
using CarWash.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CarWash.Infrastructure.Auth;

/// <summary>
/// Emissor de JWT HMAC-SHA256 (RF001 — sessão obrigatória + RT5 — RBAC futuro).
/// Stateless do lado servidor: a validação ocorre no middleware
/// <c>AddJwtBearer</c> com base nos parâmetros configurados em
/// <see cref="JwtOptions"/> + <see cref="Microsoft.IdentityModel.Tokens.TokenValidationParameters"/>.
/// </summary>
public sealed class JwtAccessTokenService : IAccessTokenService
{
    public const string ClaimPerfil = "perfil";

    private readonly JwtOptions _opcoes;
    private readonly SigningCredentials _credenciais;

    public JwtAccessTokenService(IOptions<JwtOptions> opcoes)
    {
        ArgumentNullException.ThrowIfNull(opcoes);
        _opcoes = opcoes.Value;

        if (string.IsNullOrWhiteSpace(_opcoes.Secret))
        {
            throw new InvalidOperationException(
                "JwtOptions.Secret não configurado. Defina a variável de ambiente CARWASH_JWT_SECRET.");
        }

        byte[] keyBytes = Encoding.UTF8.GetBytes(_opcoes.Secret);
        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException(
                "CARWASH_JWT_SECRET precisa ter pelo menos 32 bytes (256 bits) para HMAC-SHA256.");
        }

        var key = new SymmetricSecurityKey(keyBytes);
        _credenciais = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    /// <inheritdoc/>
    public (string Token, DateTime ExpiresAt) Emitir(Usuario usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        var agora = DateTime.UtcNow;
        var expira = agora.Add(_opcoes.AccessTokenValidade);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.EmailValor),
            new Claim(JwtRegisteredClaimNames.Name, usuario.Nome),
            new Claim(ClaimPerfil, usuario.Perfil.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim(
                JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(agora).ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64),
        };

        var token = new JwtSecurityToken(
            issuer: _opcoes.Issuer,
            audience: _opcoes.Audience,
            claims: claims,
            notBefore: agora,
            expires: expira,
            signingCredentials: _credenciais);

        var handler = new JwtSecurityTokenHandler();
        return (handler.WriteToken(token), expira);
    }
}
