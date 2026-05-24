using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CarWash.IntegrationTests.Infrastructure;

namespace CarWash.IntegrationTests.Endpoints.Agendamentos;

/// <summary>
/// Utilitário de testes de integração do RF015: monta e adultera o
/// <c>tokenConfirmacao</c> de forma determinística, reproduzindo o esquema do
/// <c>TokenConfirmacaoService</c> (<c>base64url(payloadJson).base64url(hmacSha256)</c>)
/// e usando a mesma chave configurada no <see cref="CarWashWebApplicationFactory"/>.
/// Permite gerar token expirado e token com <c>hashResumo</c> divergente sem
/// depender de relógio do sistema ou de espera real.
/// </summary>
internal static class TokenConfirmacaoTestHelper
{
    private static readonly byte[] Chave =
        Encoding.UTF8.GetBytes(CarWashWebApplicationFactory.JwtConfirmacaoTestingKey);

    /// <summary>
    /// Decompõe um token real (vindo de uma pré-confirmação) em partes
    /// estruturadas: o JSON do payload e os campos relevantes.
    /// </summary>
    public static (string PayloadJson, int V, string HashResumo, Guid UsuarioId, string TraceId, long Iat, long Exp)
        Decodificar(string token)
    {
        var partes = token.Split('.');
        if (partes.Length != 2)
        {
            throw new InvalidOperationException("Token de confirmação em formato inesperado.");
        }

        var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(partes[0]));
        using var doc = JsonDocument.Parse(payloadJson);
        var raiz = doc.RootElement;
        return (
            payloadJson,
            raiz.GetProperty("v").GetInt32(),
            raiz.GetProperty("hashResumo").GetString() ?? string.Empty,
            raiz.GetProperty("usuarioId").GetGuid(),
            raiz.GetProperty("traceId").GetString() ?? string.Empty,
            raiz.GetProperty("iat").GetInt64(),
            raiz.GetProperty("exp").GetInt64());
    }

    /// <summary>
    /// Gera um token íntegro em formato/assinatura, porém com <c>exp</c> no
    /// passado — reproduz uma sessão de confirmação expirada (espera 410). O
    /// <c>usuarioId</c> e o <c>hashResumo</c> são preservados de um token real
    /// para que apenas a expiração seja a causa da rejeição.
    /// </summary>
    public static string GerarExpiradoApartirDe(string tokenReal)
    {
        var (_, v, hashResumo, usuarioId, _, _, _) = Decodificar(tokenReal);
        var ontem = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds();
        return Montar(v, hashResumo, usuarioId, "trace-expirado", ontem, ontem + 900);
    }

    /// <summary>
    /// Gera um token íntegro e dentro da validade, porém com um <c>hashResumo</c>
    /// que não corresponde ao resumo recalculado — reproduz a divergência de
    /// resumo (espera 409 <c>agendamento-resumo-divergente</c>).
    /// </summary>
    public static string GerarComHashDivergente(string tokenReal)
    {
        var (_, v, _, usuarioId, _, _, _) = Decodificar(tokenReal);
        var agora = DateTimeOffset.UtcNow;
        var hashFalso = new string('a', 64);
        return Montar(
            v,
            hashFalso,
            usuarioId,
            "trace-divergente",
            agora.ToUnixTimeSeconds(),
            agora.AddMinutes(15).ToUnixTimeSeconds());
    }

    private static string Montar(int v, string hashResumo, Guid usuarioId, string traceId, long iat, long exp)
    {
        // Ordem fixa das chaves, igual ao PayloadInterno do TokenConfirmacaoService.
        var payloadJson =
            $"{{\"v\":{v},\"hashResumo\":\"{hashResumo}\",\"usuarioId\":\"{usuarioId}\","
            + $"\"traceId\":\"{traceId}\",\"iat\":{iat},\"exp\":{exp}}}";

        var payloadEncoded = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        using var hmac = new HMACSHA256(Chave);
        var assinatura = Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadEncoded)));
        return payloadEncoded + "." + assinatura;
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static byte[] Base64UrlDecode(string value)
    {
        var normalizado = value.Replace('-', '+').Replace('_', '/');
        switch (normalizado.Length % 4)
        {
            case 2: normalizado += "=="; break;
            case 3: normalizado += "="; break;
        }

        return Convert.FromBase64String(normalizado);
    }
}
