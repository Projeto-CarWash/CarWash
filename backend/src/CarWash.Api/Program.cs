using CarWash.Application;
using CarWash.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;

#pragma warning disable CA1861

var readyTags = new[] { "ready" };

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();

var conn = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default não configurada");

builder.Services.AddHealthChecks()
    .AddNpgSql(conn, name: "postgres", tags: readyTags);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CarWash API", Version = "v1" }));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

#pragma warning disable S6966
app.Run();
#pragma warning restore S6966
#pragma warning restore CA1861

#pragma warning disable S1118, SA1502
public partial class Program { }
#pragma warning restore S1118, SA1502
