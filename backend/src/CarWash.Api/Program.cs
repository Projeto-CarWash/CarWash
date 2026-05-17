using System.Text;
using System.Text.Json.Serialization;
using CarWash.Api.Endpoints;
using CarWash.Api.Extensions;
using CarWash.Api.Filters;
using CarWash.Api.Infrastructure;
using CarWash.Api.Middleware;
using CarWash.Application;
using CarWash.Application.Abstractions;
using CarWash.Application.Auth.Login;
using CarWash.Application.Usuarios.CriarUsuario;
using CarWash.Infrastructure;
using CarWash.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;

#pragma warning disable CA1861

const string CorsPolicyName = "CarWashClients";
var readyTags = new[] { "ready" };

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Em ambiente HTTP, sobrescreve o ICurrentRequestContext default por uma
// implementação que lê o HttpContext (claims + correlation id + IP/UA).
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentRequestContext, HttpCurrentRequestContext>();

// Serialização: enums como string (PerfilUsuario: "Admin"/"Funcionario").
builder.Services.ConfigureHttpJsonOptions(opt =>
    opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Filtros de validação genéricos (um por command/query que precisar de IValidator<T>).
builder.Services.AddScoped<ValidationFilter<CriarUsuarioCommand>>();
builder.Services.AddScoped<ValidationFilter<LoginCommand>>();
builder.Services.AddScoped<ValidationFilter<CarWash.Application.Usuarios.AlterarUsuario.AlterarUsuarioCommand>>();

// MVC controllers (ClientesController). Coexistem com os minimal API endpoints abaixo.
builder.Services.AddControllers();

// ---------- Auth: JWT Bearer ----------
var jwtConfig = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        "Configuração 'Jwt' ausente. Configure Jwt__Secret (e demais campos) via appsettings ou variáveis de ambiente.");

if (string.IsNullOrWhiteSpace(jwtConfig.Secret))
{
    throw new InvalidOperationException(
        "Jwt:Secret não configurado. Defina a variável de ambiente Jwt__Secret (≥ 32 bytes / 256 bits para HMAC-SHA256).");
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

// ---------- CORS ----------
// Frontend usa `withCredentials: true` (cookie httpOnly do refresh) — exige
// origin explícita (AllowAnyOrigin é rejeitado quando AllowCredentials é true).
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
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

var conn = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default não configurada");

builder.Services.AddHealthChecks()
    .AddNpgSql(conn, name: "postgres", tags: readyTags);

builder.Services.AddCarWashSwagger();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCarWashSwagger();

// CORS antes de Authentication (preflight não-autenticado precisa passar pelo CORS).
app.UseCors(CorsPolicyName);

app.UseAuthentication();
app.UseAuthorization();

// Sem UseHttpsRedirection: o backend roda só em HTTP (dev direto; hom/prod atrás do
// nginx que termina TLS e redireciona 80→443). Habilitar o middleware aqui emite
// warning "Failed to determine the https port" em toda request (dev) e no startup
// (hom/prod), sem efeito útil — a redireção pública é responsabilidade do proxy.
app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

app.MapCarWashEndpoints();

#pragma warning disable S6966 // Top-level statements: app.Run blocks intentionally; required by blueprint.
app.Run();
#pragma warning restore S6966
#pragma warning restore CA1861

#pragma warning disable S1118, SA1502
public partial class Program { }
#pragma warning restore S1118, SA1502
