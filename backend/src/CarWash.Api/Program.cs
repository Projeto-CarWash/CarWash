using CarWash.Api.Extensions;
using CarWash.Api.Infrastructure;
using CarWash.Api.Middleware;
using CarWash.Application;
using CarWash.Application.Abstractions;
using CarWash.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

#pragma warning disable CA1861 // Constant array passed as argument — only built once at startup.
var readyTags = new[] { "ready" };

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Em ambiente HTTP, sobrescreve o ICurrentRequestContext default por uma
// implementação que lê o HttpContext (claims + correlation id).
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentRequestContext, HttpCurrentRequestContext>();

var conn = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default não configurada");

builder.Services.AddHealthChecks()
    .AddNpgSql(conn, name: "postgres", tags: readyTags);

builder.Services.AddCarWashSwagger();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseCarWashSwagger();

app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

#pragma warning disable S6966 // Top-level statements: app.Run blocks intentionally; required by blueprint.
app.Run();
#pragma warning restore S6966
#pragma warning restore CA1861

#pragma warning disable S1118, SA1502 // Marker partial required by WebApplicationFactory<Program>.
public partial class Program { }
#pragma warning restore S1118, SA1502
