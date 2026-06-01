using CarWash.Api.Middleware;
using CarWash.Infrastructure.Auditing;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace CarWash.IntegrationTests.Middleware;

public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task Header_valido_eh_reaproveitado()
    {
        AmbientRequestContext.Reset();
        var contexto = new DefaultHttpContext();
        contexto.Request.Headers[CorrelationIdMiddleware.HeaderName] = "corr-123_ABC";

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(contexto).ConfigureAwait(false);

        string correlationId = contexto.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        correlationId.Should().Be("corr-123_ABC");
        contexto.Items[CorrelationIdMiddleware.ItemKey].Should().Be("corr-123_ABC");
    }

    [Fact]
    public async Task Header_maior_que_64_caracteres_gera_novo_id()
    {
        AmbientRequestContext.Reset();
        var contexto = new DefaultHttpContext();
        contexto.Request.Headers[CorrelationIdMiddleware.HeaderName] = new string('a', 65);

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(contexto).ConfigureAwait(false);

        string correlationId = contexto.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        correlationId.Length.Should().Be(32);
        Guid.TryParseExact(correlationId, "N", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Header_com_caracteres_invalidos_gera_novo_id()
    {
        AmbientRequestContext.Reset();
        var contexto = new DefaultHttpContext();
        contexto.Request.Headers[CorrelationIdMiddleware.HeaderName] = "corr-id-com espaço";

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(contexto).ConfigureAwait(false);

        string correlationId = contexto.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        correlationId.Length.Should().Be(32);
        Guid.TryParseExact(correlationId, "N", out _).Should().BeTrue();
    }
}
