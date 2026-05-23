namespace CarWash.Infrastructure.Auth;

/// <summary>
/// Configuração do fluxo JWT. <c>Secret</c> obrigatoriamente vem de
/// <c>CARWASH_JWT_SECRET</c> (ENV) — falha de startup em prod/hom se ausente.
/// Bind via <c>builder.Services.Configure&lt;JwtOptions&gt;(config.GetSection("Jwt"))</c>.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "carwash-api";

    public string Audience { get; set; } = "carwash-web";

    /// <summary>
    /// Chave de assinatura HMAC-SHA256. ≥ 32 bytes (256 bits). Lida de
    /// <c>CARWASH_JWT_SECRET</c> via binding — nunca commitada.
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Chave HMAC-SHA256 dedicada à assinatura do <c>tokenConfirmacao</c> da
    /// confirmação de agendamento em duas etapas (RF015 / ADR 0004). ≥ 32 bytes.
    /// Deliberadamente distinta de <see cref="Secret"/> — o token de confirmação
    /// não é um access token e não deve compartilhar material de chave. Lida de
    /// <c>Jwt__ConfirmacaoSigningKey</c> via binding; fail-fast se ausente.
    /// </summary>
    public string ConfirmacaoSigningKey { get; set; } = string.Empty;

    /// <summary>Validade do access token JWT em segundos. Default 900s = 15min.</summary>
    public int AccessTokenValiditySeconds { get; set; } = 900;

    /// <summary>Validade do refresh token em segundos. Default 604800s = 7 dias.</summary>
    public int RefreshTokenValiditySeconds { get; set; } = 604800;

    public TimeSpan AccessTokenValidade => TimeSpan.FromSeconds(AccessTokenValiditySeconds);

    public TimeSpan RefreshTokenValidade => TimeSpan.FromSeconds(RefreshTokenValiditySeconds);
}
