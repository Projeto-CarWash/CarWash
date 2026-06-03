using System.Collections.Concurrent;
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

    // Registry de schemaIds já atribuídos — desambigua dois tipos top-level com o
    // mesmo nome curto em namespaces diferentes (ex.: DTOs.CriarVeiculoRequest vs
    // Veiculos.Criar.CriarVeiculoRequest), evitando o erro
    // "The same schemaId is already used for type ...".
    private static readonly ConcurrentDictionary<Type, string> SchemaIds = new();
    private static readonly ConcurrentDictionary<string, Type> SchemaIdOwners = new(StringComparer.Ordinal);

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
    /// Habilita Swagger e Swagger UI em Development, Staging e Homologation (legado).
    /// Bloqueado em Production por padrão.
    /// </summary>
    /// <param name="app">Aplicação web já construída.</param>
    /// <returns>A própria aplicação, para encadeamento.</returns>
    public static WebApplication UseCarWashSwagger(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Environment.IsDevelopment()
            && !app.Environment.IsStaging()
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

        // SchemaIds prefixados pelo tipo "owner" — evita colisão quando dois
        // records aninhados têm o mesmo nome curto (ex.: LoginResponse.UsuarioLogado
        // vs RefreshResponse.UsuarioLogado).
        options.CustomSchemaIds(BuildSchemaId);

        AddJwtBearerSecurity(options);
        IncludeXmlCommentsIfPresent(options);
    }

    /// <summary>
    /// Gera schemaIds estáveis e únicos:
    /// <list type="bullet">
    ///   <item>Tipo top-level: usa apenas <c>Name</c> (ex.: <c>LoginResponse</c>).</item>
    ///   <item>Tipo aninhado: prefixa com o(s) declarante(s) separados por <c>.</c>
    ///   (ex.: <c>LoginResponse.UsuarioLogado</c>).</item>
    ///   <item>Genérico: substitui os colchetes por nomes encadeados
    ///   (ex.: <c>List&lt;Foo&gt;</c> → <c>ListOfFoo</c>).</item>
    /// </list>
    /// </summary>
    private static string BuildSchemaId(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return SchemaIds.GetOrAdd(type, ResolveUniqueSchemaId);
    }

    /// <summary>
    /// Resolve um schemaId único para o tipo. Usa o id "natural" quando livre e,
    /// em caso de colisão com outro tipo de nome curto idêntico, prefixa segmentos
    /// do namespace até obter unicidade (ex.: <c>DTOs.CriarVeiculoRequest</c> vs
    /// <c>Criar.CriarVeiculoRequest</c>).
    /// </summary>
    private static string ResolveUniqueSchemaId(Type type)
    {
        string baseId = ComposeSchemaId(type);

        if (TryClaimSchemaId(baseId, type, out string claimed))
        {
            return claimed;
        }

        string[] segments = (type.Namespace ?? string.Empty)
            .Split('.', StringSplitOptions.RemoveEmptyEntries);

        for (int take = 1; take <= segments.Length; take++)
        {
            string prefix = string.Join('.', segments[^take..]);
            if (TryClaimSchemaId($"{prefix}.{baseId}", type, out claimed))
            {
                return claimed;
            }
        }

        // Último recurso: nome totalmente qualificado é único por definição.
        TryClaimSchemaId(type.FullName ?? baseId, type, out claimed);
        return claimed;
    }

    private static bool TryClaimSchemaId(string candidate, Type type, out string schemaId)
    {
        schemaId = candidate;
        Type owner = SchemaIdOwners.GetOrAdd(candidate, type);
        return owner == type;
    }

    private static string ComposeSchemaId(Type type)
    {
        string Naked(Type t) => t.IsGenericType
            ? $"{t.Name.AsSpan(0, t.Name.IndexOf('`')).ToString()}Of{string.Join(string.Empty, t.GetGenericArguments().Select(ComposeSchemaId))}"
            : t.Name;

        if (type.IsNested && type.DeclaringType is not null)
        {
            return $"{ComposeSchemaId(type.DeclaringType)}.{Naked(type)}";
        }

        return Naked(type);
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
        string xmlFile = $"{assembly.GetName().Name}.xml";
        string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        }
    }
}
