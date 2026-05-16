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

    public string CorrelationId
    {
        get
        {
            var ctx = _httpContext.HttpContext;
            if (ctx is null)
            {
                return Guid.NewGuid().ToString("N");
            }

            if (ctx.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var raw)
                && raw is string id
                && !string.IsNullOrWhiteSpace(id))
            {
                return id;
            }

            var novo = Guid.NewGuid().ToString("N");
            ctx.Items[CorrelationIdMiddleware.ItemKey] = novo;
            return novo;
        }
    }

    public Guid? UsuarioId
    {
        get
        {
            var user = _httpContext.HttpContext?.User;
            if (user is null || user.Identity is null || !user.Identity.IsAuthenticated)
            {
                return null;
            }

            var raw = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("sub");

            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public string? EventoAtual
    {
        get
        {
            var ctx = _httpContext.HttpContext;
            if (ctx is null)
            {
                return null;
            }

            return ctx.Items.TryGetValue("AuditEvent", out var raw) ? raw as string : null;
        }
    }

    public void DefinirEvento(string evento)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(evento);
        var ctx = _httpContext.HttpContext
            ?? throw new InvalidOperationException("Não há HttpContext ativo para definir evento de auditoria.");
        ctx.Items["AuditEvent"] = evento;
    }
}
