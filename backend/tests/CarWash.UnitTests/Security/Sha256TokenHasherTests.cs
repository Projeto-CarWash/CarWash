using CarWash.Infrastructure.Security;
using FluentAssertions;
using Xunit;

namespace CarWash.UnitTests.Security;

public class Sha256TokenHasherTests
{
    private readonly Sha256TokenHasher _hasher = new();

    [Fact]
    public void Hash_e_estavel_para_o_mesmo_token()
    {
        var token = "refresh-token-XYZ-123";
        _hasher.Hash(token).Should().Be(_hasher.Hash(token));
    }

    [Fact]
    public void Hash_difere_para_tokens_distintos()
    {
        _hasher.Hash("token-a").Should().NotBe(_hasher.Hash("token-b"));
    }

    [Fact]
    public void Verify_retorna_true_para_token_correto()
    {
        var token = "qualquer-token-aqui";
        var hash = _hasher.Hash(token);
        _hasher.Verify(token, hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_retorna_false_para_token_invertido()
    {
        var hash = _hasher.Hash("certo");
        _hasher.Verify("errado", hash).Should().BeFalse();
    }
}
