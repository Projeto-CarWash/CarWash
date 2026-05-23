using System.IdentityModel.Tokens.Jwt;
using System.Text;
using CarWash.Domain.Entities;
using CarWash.Domain.Enums;
using CarWash.Domain.ValueObjects;
using CarWash.Infrastructure.Auth;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace CarWash.UnitTests.Infrastructure.Auth;

public class JwtAccessTokenServiceTests
{
    private const string Secret = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [Fact]
    public void Emite_token_com_claims_sub_email_name_perfil_jti_iat()
    {
        var svc = NovoServico();
        var usuario = NovoUsuario();

        var (token, expira) = svc.Emitir(usuario);

        token.Should().NotBeNullOrWhiteSpace();
        expira.Should().BeAfter(DateTime.UtcNow);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Issuer.Should().Be("carwash-api");
        jwt.Audiences.Should().Contain("carwash-web");
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == usuario.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == usuario.EmailValor);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Name && c.Value == usuario.Nome);
        jwt.Claims.Should().Contain(c => c.Type == JwtAccessTokenService.ClaimPerfil && c.Value == usuario.Perfil.ToString());
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Iat);
    }

    [Fact]
    public void Token_valida_assinatura_HMAC_SHA256()
    {
        var svc = NovoServico();
        var usuario = NovoUsuario();
        var (token, _) = svc.Emitir(usuario);

        var handler = new JwtSecurityTokenHandler();
        var parametros = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "carwash-api",
            ValidateAudience = true,
            ValidAudience = "carwash-web",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)),
            ClockSkew = TimeSpan.Zero,
        };

        var act = () => handler.ValidateToken(token, parametros, out _);
        act.Should().NotThrow();
    }

    [Fact]
    public void Secret_vazio_lanca_no_construtor()
    {
        var opts = Options.Create(new JwtOptions { Secret = string.Empty });
        Action act = () => _ = new JwtAccessTokenService(opts);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Secret_curto_lanca_no_construtor()
    {
        var opts = Options.Create(new JwtOptions { Secret = "curto-demais" });
        Action act = () => _ = new JwtAccessTokenService(opts);
        act.Should().Throw<InvalidOperationException>();
    }

    private static JwtAccessTokenService NovoServico()
    {
        var opts = Options.Create(new JwtOptions
        {
            Issuer = "carwash-api",
            Audience = "carwash-web",
            Secret = Secret,
            AccessTokenValiditySeconds = 900,
        });
        return new JwtAccessTokenService(opts);
    }

    private static Usuario NovoUsuario() =>
        Usuario.Criar(
            id: Guid.NewGuid(),
            nome: "Carol",
            email: new Email("carol@carwash.local"),
            senhaHash: "$argon2id$v=19$m=65536,t=3,p=1$c2FsdA$aGFzaA",
            perfil: PerfilUsuario.Admin);
}
