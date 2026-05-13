using CarWash.Application.Abstractions;
using CarWash.Application.Auth.Abstractions;
using CarWash.Application.Common.Security;
using CarWash.Application.Usuarios.Persistence;
using CarWash.Infrastructure.Auditing;
using CarWash.Infrastructure.Auth;
using CarWash.Infrastructure.Persistence;
using CarWash.Infrastructure.Persistence.Interceptors;
using CarWash.Infrastructure.Persistence.Repositories;
using CarWash.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CarWash.Infrastructure;

public static class DependencyInjection
{
    private const string DummyPasswordPlaintext = "__dummy_for_constant_time__";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var conn = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default não configurada");

        services.AddSingleton<IPasswordHasher, Argon2idPasswordHasher>();
        services.AddSingleton<ITokenHasher, Sha256TokenHasher>();

        // Anti-enumeration: hash dummy pré-computado uma vez no startup, usado pelo
        // LoginHandler quando o usuário não existe — nivela o tempo de resposta.
        services.AddSingleton<DummyPasswordHash>(sp =>
            new DummyPasswordHash(sp.GetRequiredService<IPasswordHasher>().Hash(DummyPasswordPlaintext)));

        services.AddSingleton<IAuthTokenService, OpaqueAuthTokenService>();

        // Contexto de auditoria — implementação default baseada em AsyncLocal.
        // A API substitui pelo CurrentRequestContext (HttpContext) no escopo da request.
        services.AddScoped<ICurrentRequestContext, AmbientRequestContext>();

        services.AddScoped<AuditableEntitiesInterceptor>();
        services.AddScoped<AuditLogInterceptor>();
        services.AddScoped<IAuditLogger, AuditLogger>();

        services.AddScoped<IUsuarioRepository, UsuarioRepository>();

        services.AddDbContext<CarWashDbContext>((sp, opt) =>
        {
            opt.UseNpgsql(conn, npg => npg
                    .MigrationsAssembly(typeof(CarWashDbContext).Assembly.FullName)
                    .MigrationsHistoryTable("__ef_migrations_history", "public"))
               .UseSnakeCaseNamingConvention()
               .AddInterceptors(
                    sp.GetRequiredService<AuditableEntitiesInterceptor>(),
                    sp.GetRequiredService<AuditLogInterceptor>());
        });

        return services;
    }
}
