using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CarWash.Application.Agendamentos.Abstractions;
using CarWash.Application.Common.Exceptions;
using CarWash.Infrastructure.Auth;
using Microsoft.Extensions.Options;

namespace CarWash.Infrastructure.Agendamentos;

/// <summary>
/// Emite e valida o <c>tokenConfirmacao</c> da confirmação em duas etapas
/// (RF015 / ADR 0004). O token é stateless, no formato
/// <c>base64url(payloadJson).base64url(hmacSha256)</c>, assinado com HMAC-SHA256
/// usando a chave dedicada <see cref="JwtOptions.ConfirmacaoSigningKey"/> — nunca
/// a chave do access token. A expiração só é avaliada depois de a assinatura
/// conferir, garantindo a distinção 400 (inválido) vs 410 (expirado).
/// </summary>
public sealed class TokenConfirmacaoService : ITokenConfirmacaoService
{
    /// <summary>Versão do esquema do payload — rejeita tokens de versão desconhecida.</summary>
    public const int VersaoPayload = 1;

    /// <summary>Janela de validade do token (15 min) — ADR 0004.</summary>
    public static readonly TimeSpan Validade = TimeSpan.FromMinutes(15);

    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    private readonly byte[] _chave;

    public TokenConfirmacaoService(IOptions<JwtOptions> opcoes)
    {
        ArgumentNullException.ThrowIfNull(opcoes);

        string chave = opcoes.Value.ConfirmacaoSigningKey;
        if (string.IsNullOrWhiteSpace(chave))
        {
            throw new InvalidOperationException(
                "Jwt:ConfirmacaoSigningKey não configurada. Defina Jwt__ConfirmacaoSigningKey (≥ 32 bytes) "
                + "para a assinatura do token de confirmação de agendamento (RF015).");
        }

        byte[] bytes = Encoding.UTF8.GetBytes(chave);
        if (bytes.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:ConfirmacaoSigningKey precisa ter pelo menos 32 bytes (256 bits) para HMAC-SHA256.");
        }

        _chave = bytes;
    }

    /// <inheritdoc/>
    public string Gerar(string hashResumo, Guid usuarioId, string traceId)
    {
        if (string.IsNullOrWhiteSpace(hashResumo))
        {
            throw new ArgumentException("hashResumo é obrigatório.", nameof(hashResumo));
        }

        if (usuarioId == Guid.Empty)
        {
            throw new ArgumentException("usuarioId é obrigatório.", nameof(usuarioId));
        }

        var agora = DateTimeOffset.UtcNow;
        long iat = agora.ToUnixTimeSeconds();
        long exp = agora.Add(Validade).ToUnixTimeSeconds();

        var payload = new PayloadInterno
        {
            V = VersaoPayload,
            HashResumo = hashResumo,
            UsuarioId = usuarioId,
            TraceId = traceId ?? string.Empty,
            Iat = iat,
            Exp = exp,
        };

        string payloadJson = JsonSerializer.Serialize(payload, PayloadJsonOptions);
        string payloadEncoded = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        string assinatura = Base64UrlEncode(AssinarUtf8(payloadEncoded));

        return payloadEncoded + "." + assinatura;
    }

    /// <inheritdoc/>
    public TokenConfirmacaoPayload Validar(string token, Guid usuarioAutenticadoId)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new TokenConfirmacaoInvalidoException();
        }

        // Formato: exatamente duas partes separadas por '.'.
        string[] partes = token.Split('.');
        if (partes.Length != 2 || partes[0].Length == 0 || partes[1].Length == 0)
        {
            throw new TokenConfirmacaoInvalidoException();
        }

        byte[] payloadBytes;
        byte[] assinaturaInformada;
        try
        {
            payloadBytes = Base64UrlDecode(partes[0]);
            assinaturaInformada = Base64UrlDecode(partes[1]);
        }
        catch (FormatException ex)
        {
            throw new TokenConfirmacaoInvalidoException(ex);
        }

        // HMAC sobre a string base64url do payload — comparação de tempo fixo.
        byte[] assinaturaEsperada = AssinarUtf8(partes[0]);
        if (!CryptographicOperations.FixedTimeEquals(assinaturaInformada, assinaturaEsperada))
        {
            throw new TokenConfirmacaoInvalidoException();
        }

        PayloadInterno? payload;
        try
        {
            payload = JsonSerializer.Deserialize<PayloadInterno>(payloadBytes, PayloadJsonOptions);
        }
        catch (JsonException ex)
        {
            throw new TokenConfirmacaoInvalidoException(ex);
        }

        if (payload is null
            || payload.V != VersaoPayload
            || string.IsNullOrWhiteSpace(payload.HashResumo)
            || payload.UsuarioId == Guid.Empty)
        {
            throw new TokenConfirmacaoInvalidoException();
        }

        // usuarioId divergente do `sub` autenticado → 400 (token de outra sessão).
        if (payload.UsuarioId != usuarioAutenticadoId)
        {
            throw new TokenConfirmacaoInvalidoException();
        }

        // A expiração só é avaliada DEPOIS de a assinatura/formato conferirem.
        var expiraEm = DateTimeOffset.FromUnixTimeSeconds(payload.Exp).UtcDateTime;
        if (expiraEm <= DateTime.UtcNow)
        {
            throw new SessaoConfirmacaoExpiradaException();
        }

        return new TokenConfirmacaoPayload(
            payload.HashResumo,
            payload.UsuarioId,
            payload.TraceId ?? string.Empty,
            expiraEm);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

    private static byte[] Base64UrlDecode(string value)
    {
        string normalizado = value.Replace('-', '+').Replace('_', '/');
        switch (normalizado.Length % 4)
        {
            case 2: normalizado += "=="; break;
            case 3: normalizado += "="; break;
            case 1: throw new FormatException("Base64url inválido.");
        }

        return Convert.FromBase64String(normalizado);
    }

    private byte[] AssinarUtf8(string conteudo)
    {
        using var hmac = new HMACSHA256(_chave);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(conteudo));
    }

    /// <summary>
    /// Esquema interno do payload do token. Ordem fixa de chaves em camelCase
    /// (<c>v, hashResumo, usuarioId, traceId, iat, exp</c>) — ADR 0004.
    /// </summary>
    private sealed class PayloadInterno
    {
        [JsonPropertyName("v")]
        [JsonPropertyOrder(1)]
        public int V { get; set; }

        [JsonPropertyName("hashResumo")]
        [JsonPropertyOrder(2)]
        public string HashResumo { get; set; } = string.Empty;

        [JsonPropertyName("usuarioId")]
        [JsonPropertyOrder(3)]
        public Guid UsuarioId { get; set; }

        [JsonPropertyName("traceId")]
        [JsonPropertyOrder(4)]
        public string TraceId { get; set; } = string.Empty;

        [JsonPropertyName("iat")]
        [JsonPropertyOrder(5)]
        public long Iat { get; set; }

        [JsonPropertyName("exp")]
        [JsonPropertyOrder(6)]
        public long Exp { get; set; }
    }
}
