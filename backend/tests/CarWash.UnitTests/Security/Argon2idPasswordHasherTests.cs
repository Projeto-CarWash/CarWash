using CarWash.Infrastructure.Security;
using FluentAssertions;
using Xunit;

namespace CarWash.UnitTests.Security;

public class Argon2idPasswordHasherTests
{
    private readonly Argon2idPasswordHasher _hasher = new();

    [Fact]
    public void Hash_devolve_PHC_canonico_com_parametros_documentados()
    {
        string hash = _hasher.Hash("senhaForte123!");
        hash.Should().StartWith("$argon2id$v=19$m=65536,t=3,p=1$");
        hash.Split('$').Should().HaveCount(6);
    }

    [Fact]
    public void Verify_retorna_true_para_senha_correta()
    {
        string hash = _hasher.Hash("abc123XYZ!");
        _hasher.Verify("abc123XYZ!", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_retorna_false_para_senha_errada()
    {
        string hash = _hasher.Hash("abc123XYZ!");
        _hasher.Verify("outraSenha", hash).Should().BeFalse();
    }

    [Fact]
    public void Verify_retorna_false_para_hash_invalido()
    {
        _hasher.Verify("qualquer", "$argon2id$bagunca").Should().BeFalse();
        _hasher.Verify("qualquer", string.Empty).Should().BeFalse();
    }

    [Fact]
    public void NeedsRehash_true_quando_parametros_do_hash_sao_mais_fracos()
    {
        // Hash artificial com m=1024 (mais fraco que o atual 65536). Não conseguimos
        // verificá-lo de fato (salt/hash são placeholders), mas NeedsRehash apenas lê
        // os parâmetros do PHC — válido para o teste.
        string hashFraco = "$argon2id$v=19$m=1024,t=2,p=1$YWFhYWFhYWFhYWFhYWFhYQ$YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWE";
        _hasher.NeedsRehash(hashFraco).Should().BeTrue();
    }

    [Fact]
    public void NeedsRehash_false_para_hash_recem_gerado()
    {
        string hash = _hasher.Hash("teste");
        _hasher.NeedsRehash(hash).Should().BeFalse();
    }
}
