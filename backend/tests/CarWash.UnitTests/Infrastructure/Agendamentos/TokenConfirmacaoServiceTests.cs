using CarWash.Application.Common.Exceptions;
using CarWash.Infrastructure.Agendamentos;
using CarWash.Infrastructure.Auth;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CarWash.UnitTests.Infrastructure.Agendamentos;

public class TokenConfirmacaoServiceTests
{
    private const string ChaveValida = "chave-de-confirmacao-rf015-com-mais-de-32-bytes-para-hmac";
    private const string ChaveAlternativa = "outra-chave-de-confirmacao-rf015-distinta-com-32-bytes-min";
    private const string HashResumo = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    private static readonly Guid Usuario = Guid.NewGuid();

    [Fact]
    public void Gerar_e_validar_token_devolve_payload_integro()
    {
        var servico = NovoServico(ChaveValida);

        var token = servico.Gerar(HashResumo, Usuario, "trace-1");
        var payload = servico.Validar(token, Usuario);

        payload.HashResumo.Should().Be(HashResumo);
        payload.UsuarioId.Should().Be(Usuario);
        payload.TraceId.Should().Be("trace-1");
        payload.ExpiraEm.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(5));
        payload.ExpiraEm.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Token_tem_formato_de_duas_partes_base64url()
    {
        var servico = NovoServico(ChaveValida);

        var token = servico.Gerar(HashResumo, Usuario, "trace-1");

        token.Split('.').Should().HaveCount(2);
        token.Should().NotContain("+").And.NotContain("/").And.NotContain("=");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sem-ponto")]
    [InlineData("partes.demais.aqui")]
    [InlineData(".vazio")]
    [InlineData("vazio.")]
    public void Token_com_formato_invalido_lanca_TokenConfirmacaoInvalido(string token)
    {
        var servico = NovoServico(ChaveValida);

        var act = () => servico.Validar(token, Usuario);

        act.Should().Throw<TokenConfirmacaoInvalidoException>();
    }

    [Fact]
    public void Token_com_base64_invalido_lanca_TokenConfirmacaoInvalido()
    {
        var servico = NovoServico(ChaveValida);

        var act = () => servico.Validar("@@@.@@@", Usuario);

        act.Should().Throw<TokenConfirmacaoInvalidoException>();
    }

    [Fact]
    public void Token_assinado_com_outra_chave_lanca_TokenConfirmacaoInvalido()
    {
        var emissor = NovoServico(ChaveValida);
        var verificador = NovoServico(ChaveAlternativa);

        var token = emissor.Gerar(HashResumo, Usuario, "trace-1");
        var act = () => verificador.Validar(token, Usuario);

        act.Should().Throw<TokenConfirmacaoInvalidoException>();
    }

    [Fact]
    public void Token_com_payload_adulterado_lanca_TokenConfirmacaoInvalido()
    {
        var servico = NovoServico(ChaveValida);

        var token = servico.Gerar(HashResumo, Usuario, "trace-1");
        var partes = token.Split('.');

        // Adultera o payload mantendo a assinatura original — HMAC deve falhar.
        var adulterado = partes[0][..^2] + "XY." + partes[1];

        var act = () => servico.Validar(adulterado, Usuario);

        act.Should().Throw<TokenConfirmacaoInvalidoException>();
    }

    [Fact]
    public void Token_de_outro_usuario_lanca_TokenConfirmacaoInvalido()
    {
        var servico = NovoServico(ChaveValida);

        var token = servico.Gerar(HashResumo, Usuario, "trace-1");
        var act = () => servico.Validar(token, Guid.NewGuid());

        act.Should().Throw<TokenConfirmacaoInvalidoException>();
    }

    [Fact]
    public void Token_expirado_lanca_SessaoConfirmacaoExpirada()
    {
        // Token assinado corretamente, porém com exp no passado: a assinatura
        // confere primeiro e só então a expiração é avaliada → 410, não 400.
        var servico = NovoServico(ChaveValida);
        var tokenExpirado = TokenExpiradoSintetico(ChaveValida, Usuario);

        var act = () => servico.Validar(tokenExpirado, Usuario);

        act.Should().Throw<SessaoConfirmacaoExpiradaException>();
    }

    [Fact]
    public void Token_expirado_mas_de_outra_chave_lanca_TokenConfirmacaoInvalido()
    {
        // Mesmo expirado, a assinatura é verificada ANTES da expiração. Chave
        // errada → 400 (inválido), nunca 410.
        var verificador = NovoServico(ChaveAlternativa);
        var tokenExpirado = TokenExpiradoSintetico(ChaveValida, Usuario);

        var act = () => verificador.Validar(tokenExpirado, Usuario);

        act.Should().Throw<TokenConfirmacaoInvalidoException>();
    }

    [Fact]
    public void Construtor_sem_chave_falha_rapido()
    {
        var act = () => NovoServico(string.Empty);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Construtor_com_chave_curta_falha_rapido()
    {
        var act = () => NovoServico("curta");

        act.Should().Throw<InvalidOperationException>();
    }

    private static TokenConfirmacaoService NovoServico(string chaveConfirmacao)
    {
        var opcoes = Options.Create(new JwtOptions
        {
            Secret = "secret-do-access-token-distinto-com-mais-de-32-bytes",
            ConfirmacaoSigningKey = chaveConfirmacao,
        });
        return new TokenConfirmacaoService(opcoes);
    }

    /// <summary>
    /// Reconstrói um token válido em formato/assinatura mas com <c>exp</c> no
    /// passado, reproduzindo o esquema do <see cref="TokenConfirmacaoService"/>.
    /// </summary>
    private static string TokenExpiradoSintetico(string chave, Guid usuario)
    {
        var ontem = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds();
        var payloadJson =
            $"{{\"v\":1,\"hashResumo\":\"{HashResumo}\",\"usuarioId\":\"{usuario}\","
            + $"\"traceId\":\"trace-old\",\"iat\":{ontem},\"exp\":{ontem + 900}}}";

        var payloadEncoded = Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(payloadJson));
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(chave));
        var assinatura = Base64UrlEncode(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payloadEncoded)));
        return payloadEncoded + "." + assinatura;
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
