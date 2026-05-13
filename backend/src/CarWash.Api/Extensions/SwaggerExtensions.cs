using System.Reflection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CarWash.Api.Extensions;

/// <summary>
/// Configuração centralizada do Swagger/OpenAPI da CarWash API.
/// Mantém <c>Program.cs</c> enxuto (DAT §3.1, §4.2).
/// </summary>
internal static class SwaggerExtensions
{
    private const string DocumentName = "v1";
    private const string HomologationEnvironment = "Homologation";

    /// <summary>
    /// Registra o gerador OpenAPI com metadados completos, suporte a JWT Bearer
    /// e inclusão de comentários XML quando disponíveis.
    /// </summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    /// <returns>A própria coleção, para encadeamento.</returns>
    public static IServiceCollection AddCarWashSwagger(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(ConfigureSwaggerGen);

        return services;
    }

    /// <summary>
    /// Habilita Swagger e Swagger UI em Development e Homologation.
    /// Bloqueado em Production por padrão.
    /// </summary>
    /// <param name="app">Aplicação web já construída.</param>
    /// <returns>A própria aplicação, para encadeamento.</returns>
    public static WebApplication UseCarWashSwagger(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Environment.IsDevelopment()
            && !app.Environment.IsEnvironment(HomologationEnvironment))
        {
            return app;
        }

        app.UseSwagger();
        app.UseSwaggerUI(ui =>
        {
            // JSON continua servido por UseSwagger no caminho padrão /swagger/{doc}/swagger.json.
            // Como a UI está em /docs, usar caminho relativo preserva prefixes de proxy
            // reverso (ex.: /api/docs -> /api/swagger/{doc}/swagger.json).
            ui.SwaggerEndpoint($"../swagger/{DocumentName}/swagger.json", $"CarWash API {DocumentName}");
            ui.RoutePrefix = "docs";
            ui.DocumentTitle = "CarWash API";
            ui.DefaultModelsExpandDepth(-1);
            ui.DisplayRequestDuration();
        });

        return app;
    }

    private static void ConfigureSwaggerGen(SwaggerGenOptions options)
    {
        options.SwaggerDoc(
            DocumentName,
            new OpenApiInfo
            {
                Title = "CarWash API",
                Version = DocumentName,
                Description =
                    "API do CarWash — sistema de gestão de lava-rápido (MVP). " +
                    "Cobre cadastro de clientes, veículos, filiais, serviços e agendamentos.",
                Contact = new OpenApiContact
                {
                    Name = "Equipe CarWash",
                },

                // TCC universitário: licença de uso acadêmico — não há intenção
                // de distribuição open-source neste momento.
                License = new OpenApiLicense
                {
                    Name = "Uso acadêmico",
                },
            });

        // Servidor relativo evita URLs absolutas erradas atrás de proxy reverso (DAT §8).
        options.AddServer(new OpenApiServer
        {
            Url = "/",
            Description = "Servidor atual",
        });

        AddJwtBearerSecurity(options);
        IncludeXmlCommentsIfPresent(options);
    }

    private static void AddJwtBearerSecurity(SwaggerGenOptions options)
    {
        const string securitySchemeId = "Bearer";

        options.AddSecurityDefinition(
            securitySchemeId,
            new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Insira o token JWT (sem o prefixo 'Bearer').",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
            });

        var bearerScheme = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = securitySchemeId,
            },
        };

        var requirement = new OpenApiSecurityRequirement();
        requirement.Add(bearerScheme, Array.Empty<string>());

        options.AddSecurityRequirement(requirement);
    }

    private static void IncludeXmlCommentsIfPresent(SwaggerGenOptions options)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var xmlFile = $"{assembly.GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        }
    }
}
