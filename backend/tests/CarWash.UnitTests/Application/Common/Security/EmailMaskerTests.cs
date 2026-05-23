using CarWash.Application.Common.Security;
using FluentAssertions;
using Xunit;

namespace CarWash.UnitTests.Application.Common.Security;

public class EmailMaskerTests
{
    [Theory]
    [InlineData(null, "***@***")]
    [InlineData("", "***@***")]
    [InlineData("  ", "***@***")]
    [InlineData("semarroba", "***@***")]
    [InlineData("a@b.com", "***@***")] // parte local ≤ 2 chars
    [InlineData("ab@b.com", "***@***")] // exatamente 2 chars ainda mascara tudo
    [InlineData("abc@b.com", "ab***@b.com")]
    [InlineData("guilherme@empresa.com", "gu***@empresa.com")]
    [InlineData("ALICE@CARWASH.LOCAL", "AL***@CARWASH.LOCAL")]
    public void Mascara_email_conforme_politica(string? entrada, string esperado)
        => EmailMasker.Mask(entrada).Should().Be(esperado);
}
