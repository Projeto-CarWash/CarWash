using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using CarWash.Api.Endpoints;
using CarWash.Api.Extensions;
using CarWash.Api.Filters;
using CarWash.Api.Infrastructure;
using CarWash.Api.Middleware;
using CarWash.Application;
using CarWash.Application.Abstractions;
using CarWash.Infrastructure;
using CarWash.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Serilog;

#pragma warning disable CA1861

const string CorsPolicyName = "CarWashClients";
string[] readyTags = new[] { "ready" };

var builder = WebApplication.CreateBuilder(args);

// ---------- Logging (Serilog) ----------
// Lê configuração de "Serilog" do appsettings; em prod/hom o docker-compose
// pode montar volume em /app/logs para sink File (não habilitado por padrão).
builder.Host.UseSerilog((ctx, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.InvariantCulture);
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();

// Em ambiente HTTP, sobrescreve o ICurrentRequestContext default por uma
// implementação que lê o HttpContext (claims + correlation id + IP/UA).
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentRequestContext, HttpCurrentRequestContext>();

// Serialização: enums como string (PerfilUsuario: "Admin"/"Funcionario").
builder.Services.ConfigureHttpJsonOptions(opt =>
    opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Filtros de validação genéricos: scan automático do assembly da Application
// — registra ValidationFilter<T> fechado para cada Command/Query com IValidator<T>.
builder.Services.AddValidationFilters(typeof(CarWash.Application.DependencyInjection).Assembly);

// ---------- Auth: JWT Bearer ----------
var jwtConfig = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        "Configuração 'Jwt' ausente. Configure Jwt__Secret (e demais campos) via appsettings ou variáveis de ambiente.");

if (string.IsNullOrWhiteSpace(jwtConfig.Secret))
{
    throw new InvalidOperationException(
        "Jwt:Secret não configurado. Defina a variável de ambiente Jwt__Secret (≥ 32 bytes / 256 bits para HMAC-SHA256).");
}

// RF015 / ADR 0004: chave dedicada do token de confirmação de agendamento.
// Fail-fast no startup — não esperar a primeira pré-confirmação para descobrir.
if (string.IsNullOrWhiteSpace(jwtConfig.ConfirmacaoSigningKey))
{
    throw new InvalidOperationException(
        "Jwt:ConfirmacaoSigningKey não configurada. Defina a variável de ambiente Jwt__ConfirmacaoSigningKey "
        + "(≥ 32 bytes / 256 bits para HMAC-SHA256) — chave dedicada do token de confirmação (RF015).");
}

var jwtKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Secret));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtConfig.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtConfig.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwtKey,
            NameClaimType = "name",
            RoleClaimType = "perfil",
        };
    });

builder.Services.AddAuthorization();

// ---------- Rate limiting ----------
// Defesa em profundidade contra força-bruta no /auth/login (RF001 já tem lockout
// por usuário; o rate-limit por IP cobre o cenário "N emails diferentes pelo mesmo IP").
builder.Services.AddRateLimiter(opt =>
{
    opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opt.OnRejected = async (ctx, ct) =>
    {
        if (ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            ctx.HttpContext.Response.Headers["Retry-After"] =
                ((int)retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        ctx.HttpContext.Response.ContentType = "application/problem+json";
        await ctx.HttpContext.Response.WriteAsync(
            "{\"title\":\"Muitas tentativas. Aguarde um instante e tente novamente.\",\"status\":429}",
            ct).ConfigureAwait(false);
    };

    // Limite por janela configurável (RateLimiting:AuthLoginPermitLimit). Default 10/min
    // em produção; o ambiente E2E sobe o valor para caber a suíte completa numa única
    // execução sem 429 — a stack roda atrás de um único IP/proxy, então todos os logins
    // dos specs caem na mesma partição.
    int authLoginPermitLimit =
        builder.Configuration.GetValue("RateLimiting:AuthLoginPermitLimit", 10);
    opt.AddPolicy("auth-login", http =>
    {
        string key = http.Connection.RemoteIpAddress?.ToString() ?? "anonimo";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = authLoginPermitLimit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true,
        });
    });
});

// ---------- CORS ----------
// Frontend usa `withCredentials: true` (cookie httpOnly do refresh) — exige
// origin explícita (AllowAnyOrigin é rejeitado quando AllowCredentials é true).
string[] corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(opt =>
{
    opt.AddPolicy(CorsPolicyName, policy =>
    {
        if (corsOrigins.Length > 0)
        {
            policy.WithOrigins(corsOrigins);
        }

        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

string conn = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default não configurada");

builder.Services.AddHealthChecks()
    .AddNpgSql(conn, name: "postgres", tags: readyTags);

builder.Services.AddCarWashSwagger();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// BUG-CONTRATO-404-ROUTE: padroniza 404 que escapa do roteador (route constraint
// não bate, ex.: `{id:guid}` com "abc") em ProblemDetails canônico. NÃO sobrescreve
// 404 que já produziu corpo (NotFoundException via ExceptionHandlingMiddleware) —
// a condição `Response.HasStarted || ContentLength > 0` cobre isso.
app.UseStatusCodePages(async ctx =>
{
    var response = ctx.HttpContext.Response;
    if (response.StatusCode != StatusCodes.Status404NotFound
        || response.HasStarted
        || (response.ContentLength is > 0))
    {
        return;
    }

    string correlationId = ctx.HttpContext.Items[CorrelationIdMiddleware.ItemKey] as string
        ?? Guid.NewGuid().ToString("N");

    response.ContentType = "application/problem+json";
    response.Headers.CacheControl = "no-store";

    var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
    {
        Type = "https://carwash/errors/not-found",
        Title = "Recurso não encontrado.",
        Status = StatusCodes.Status404NotFound,
    };
    problem.Extensions["correlationId"] = correlationId;

    await response.WriteAsync(
        System.Text.Json.JsonSerializer.Serialize(problem, CarWash.Api.NotFoundProblemJsonOptions.Default))
        .ConfigureAwait(false);
});

app.UseCarWashSwagger();

// CORS antes de Authentication (preflight não-autenticado precisa passar pelo CORS).
app.UseCors(CorsPolicyName);

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Sem UseHttpsRedirection: o backend roda só em HTTP (dev direto; hom/prod atrás do
// nginx que termina TLS e redireciona 80→443). Habilitar o middleware aqui emite
// warning "Failed to determine the https port" em toda request (dev) e no startup
// (hom/prod), sem efeito útil — a redireção pública é responsabilidade do proxy.
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

app.MapCarWashEndpoints();
app.MapControllers();

#pragma warning disable S6966 // Top-level statements: app.Run blocks intentionally; required by blueprint.
app.Run();
#pragma warning restore S6966
#pragma warning restore CA1861

#pragma warning disable S1118, SA1502
public partial class Program { }
#pragma warning restore S1118, SA1502

#pragma warning disable SA1402, SA1403 // Tipo auxiliar para o handler do UseStatusCodePages (cache de JsonSerializerOptions, CA1869).
namespace CarWash.Api
{
    internal static class NotFoundProblemJsonOptions
    {
        public static readonly System.Text.Json.JsonSerializerOptions Default =
            new(System.Text.Json.JsonSerializerDefaults.Web);
    }
}
#pragma warning restore SA1402, SA1403
