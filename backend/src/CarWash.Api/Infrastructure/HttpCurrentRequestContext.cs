using System.Security.Claims;
using CarWash.Api.Middleware;
using CarWash.Application.Abstractions;

namespace CarWash.Api.Infrastructure;

/// <summary>
/// Implementação de <see cref="ICurrentRequestContext"/> baseada em
/// <see cref="IHttpContextAccessor"/> — preenche <c>CorrelationId</c> a partir
/// do middleware e <c>UsuarioId</c> a partir das claims JWT (DB001 §06.3.2).
/// </summary>
public sealed class HttpCurrentRequestContext : ICurrentRequestContext
{
    private readonly IHttpContextAccessor _httpContext;

    public HttpCurrentRequestContext(IHttpContextAccessor httpContext)
    {
        _httpContext = httpContext;
    }

    /// <inheritdoc/>
    public string CorrelationId
    {
        get
        {
            var ctx = _httpContext.HttpContext;
            if (ctx is null)
            {
                return Guid.NewGuid().ToString("N");
            }

            if (ctx.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out object? raw)
                && raw is string id
                && !string.IsNullOrWhiteSpace(id))
            {
                return id;
            }

            string novo = Guid.NewGuid().ToString("N");
            ctx.Items[CorrelationIdMiddleware.ItemKey] = novo;
            return novo;
        }
    }

    /// <inheritdoc/>
    public Guid? UsuarioId
    {
        get
        {
            var user = _httpContext.HttpContext?.User;
            if (user is null || user.Identity is null || !user.Identity.IsAuthenticated)
            {
                return null;
            }

            string? raw = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("sub");

            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    /// <inheritdoc/>
    public string? EventoAtual
    {
        get
        {
            var ctx = _httpContext.HttpContext;
            if (ctx is null)
            {
                return null;
            }

            return ctx.Items.TryGetValue("AuditEvent", out object? raw) ? raw as string : null;
        }
    }

    /// <inheritdoc/>
    public void DefinirEvento(string evento)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(evento);
        var ctx = _httpContext.HttpContext
            ?? throw new InvalidOperationException("Não há HttpContext ativo para definir evento de auditoria.");
        ctx.Items["AuditEvent"] = evento;
    }

    /// <inheritdoc/>
    public string? IpOrigem
    {
        get
        {
            var ctx = _httpContext.HttpContext;
            if (ctx is null)
            {
                return null;
            }

            // Em prod/hom o backend roda atrás do nginx, que injeta o IP real em
            // X-Forwarded-For. Em dev direto, cai no RemoteIpAddress.
            if (ctx.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded)
                && !string.IsNullOrWhiteSpace(forwarded.ToString()))
            {
                string first = forwarded.ToString().Split(',', 2)[0].Trim();
                if (!string.IsNullOrWhiteSpace(first))
                {
                    return first;
                }
            }

            return ctx.Connection.RemoteIpAddress?.ToString();
        }
    }

    /// <inheritdoc/>
    public string? UserAgent
    {
        get
        {
            var ctx = _httpContext.HttpContext;
            if (ctx is null)
            {
                return null;
            }

            string ua = ctx.Request.Headers["User-Agent"].ToString();
            if (string.IsNullOrWhiteSpace(ua))
            {
                return null;
            }

            // Trunca em 255 chars para alinhar com a coluna usuario_sessoes.user_agent
            // (DB001 §06.5) e evitar exceções de overflow ao persistir a sessão.
            const int MaxLen = 255;
            return ua.Length > MaxLen ? ua[..MaxLen] : ua;
        }
    }
}
