using System.Text;
using CarWash.Api.Middlewares;
using CarWash.Application;
using CarWash.Application.DTOs;
using CarWash.Infrastructure; // <-- ADICIONADO PARA CORRIGIR O ERRO 1
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

#pragma warning disable CA1861 // Constant array passed as argument - only built once at startup.
string[] readyTags = new[] { "ready" };

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(entry.Key),
                entry => entry.Value!.Errors
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "Campo inválido."
                        : error.ErrorMessage)
                    .ToArray());

        var response = new BaseResponse
        {
            Message = "Dados de acesso inválidos. Verifique os campos e tente novamente.",
            Code = "AUTH_VALIDATION_ERROR",
            TraceId = context.HttpContext.TraceIdentifier,
            Errors = errors
        };

        return new BadRequestObjectResult(response);
    };
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration); // <-- AGORA ELE ACHA ISSO!

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
string secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret não configurado.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

string conn = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default não configurada");

builder.Services.AddHealthChecks()
    .AddNpgSql(conn, name: "postgres", tags: readyTags);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CarWash API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o token JWT desta maneira: Bearer {seu_token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

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

