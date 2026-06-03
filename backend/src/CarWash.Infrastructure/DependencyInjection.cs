using CarWash.Application.Abstractions;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Agenda.Persistence;
using CarWash.Application.Agendamentos.Abstractions;
using CarWash.Application.Agendamentos.Persistence;
using CarWash.Application.Auth.Abstractions;
using CarWash.Application.Auth.Persistence;
using CarWash.Application.Clientes.Persistence;
using CarWash.Application.Common.Security;
using CarWash.Application.Filiais.Persistence;
using CarWash.Application.Interfaces;
using CarWash.Application.Responsaveis.Persistence;
using CarWash.Application.Servicos.Persistence;
using CarWash.Application.Usuarios.Persistence;
using CarWash.Application.Veiculos.Persistence;
using CarWash.Infrastructure.Agendamentos;
using CarWash.Infrastructure.Auditing;
using CarWash.Infrastructure.Auth;
using CarWash.Infrastructure.Persistence;
using CarWash.Infrastructure.Persistence.Interceptors;
using CarWash.Infrastructure.Persistence.Maintenance;
using CarWash.Infrastructure.Persistence.Repositories;
using CarWash.Infrastructure.Repositories;
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

        string conn = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default não configurada");

        services.AddSingleton<IPasswordHasher, Argon2idPasswordHasher>();
        services.AddSingleton<ITokenHasher, Sha256TokenHasher>();

        // Anti-enumeration: hash dummy pré-computado uma vez no startup, usado pelo
        // LoginHandler quando o usuário não existe — nivela o tempo de resposta.
        services.AddSingleton<DummyPasswordHash>(sp =>
            new DummyPasswordHash(sp.GetRequiredService<IPasswordHasher>().Hash(DummyPasswordPlaintext)));

        // Auth: JWT access (HMAC-SHA256, singleton) + refresh persistido em UsuarioSessao (scoped).
        // ADR 0002 §Implicações: refresh_token_hash = SHA-256 via ITokenHasher.
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IAccessTokenService, JwtAccessTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IUsuarioSessaoRepository, UsuarioSessaoRepository>();

        // Contexto de auditoria — implementação default baseada em AsyncLocal.
        // A API substitui pelo CurrentRequestContext (HttpContext) no escopo da request.
        services.AddScoped<ICurrentRequestContext, AmbientRequestContext>();

        services.AddScoped<AuditableEntitiesInterceptor>();
        services.AddScoped<AuditLogInterceptor>();
        services.AddScoped<IAuditLogger, AuditLogger>();

        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IFilialRepository, FilialRepository>();
        services.AddScoped<IAgendamentoRepository, AgendamentoRepository>();
        services.AddScoped<IAgendamentoCatalogoRepository, AgendamentoCatalogoRepository>();
        services.AddScoped<IIdempotenciaRepository, IdempotenciaRepository>();
        services.AddScoped<IServicoRepository, ServicoRepository>();
        services.AddScoped<IAgendaRepository, AgendaRepository>();
        services.AddScoped<IVeiculoRepository, VeiculoRepository>();
        services.AddScoped<IResponsavelRepository, ResponsavelRepository>();
        services.AddScoped<IAgendamentoObservacaoRepository, AgendamentoObservacaoRepository>();

        // RF015 — confirmação de agendamento em duas etapas (ADR 0004).
        // Token de confirmação: singleton (sem estado mutável; só lê a chave HMAC).
        services.AddSingleton<ITokenConfirmacaoService, TokenConfirmacaoService>();

        // Limpeza diária dos registros de idempotência expirados (janela 24h).
        services.AddHostedService<IdempotenciaCleanupService>();

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

        services.AddScoped<IHistoricoAtendimentosClienteRepository, HistoricoAtendimentosClienteRepository>();

        // IDbContextFactory para casos que precisam de um DbContext fora do escopo
        // da request (ex.: AuditLogger). Construímos as options manualmente — sem
        // interceptors — para não conflitar com a registração Scoped de
        // DbContextOptions<CarWashDbContext> feita acima por AddDbContext.
        // Audits não devem auditar a si próprios, então a ausência dos
        // interceptors aqui é intencional.
        var factoryOptions = new DbContextOptionsBuilder<CarWashDbContext>()
            .UseNpgsql(conn, npg => npg
                .MigrationsAssembly(typeof(CarWashDbContext).Assembly.FullName)
                .MigrationsHistoryTable("__ef_migrations_history", "public"))
            .UseSnakeCaseNamingConvention()
            .Options;

        services.AddSingleton<IDbContextFactory<CarWashDbContext>>(
            new CarWashRuntimeDbContextFactory(factoryOptions));

        services.AddScoped<ICarWashDbContext, CarWashDbContext>();

        return services;
    }
}
