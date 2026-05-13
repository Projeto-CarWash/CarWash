using CarWash.Infrastructure.Auditing;
using Serilog.Context;

namespace CarWash.Api.Middleware;

/// <summary>
/// Lê/gera o header <c>X-Correlation-Id</c>, propaga para <c>HttpContext.Items</c>,
/// para o <see cref="LogContext"/> do Serilog e para o <see cref="AmbientRequestContext"/>
/// — disponível em qualquer ponto da pipeline (DB001 §06.3).
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = ResolveCorrelationId(context);
        context.Items[ItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        AmbientRequestContext.DefinirCorrelationId(correlationId);

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context).ConfigureAwait(false);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var valor)
            && !string.IsNullOrWhiteSpace(valor.ToString()))
        {
            return valor.ToString();
        }

        return Guid.NewGuid().ToString("N");
    }
}
