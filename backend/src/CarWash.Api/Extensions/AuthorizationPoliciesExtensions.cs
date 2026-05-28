using Microsoft.Extensions.DependencyInjection;

namespace CarWash.Api.Extensions;

/// <summary>
/// Policies de autorização do CarWash. Centraliza a configuração para que rotas
/// privilegiadas referenciem o nome da policy em vez de <c>RequireRole(...)</c>
/// espalhado — facilita evolução do RBAC e telemetria (RT5 do DAT).
/// </summary>
public static class AuthorizationPoliciesExtensions
{
    /// <summary>
    /// Policy nominal de administrador. Equivale a <c>RequireRole("Admin")</c>
    /// sobre o claim <c>perfil</c> (RoleClaimType configurado em <c>Program.cs</c>).
    /// </summary>
    public const string AdminPolicy = "Admin";

    public static IServiceCollection AddCarWashAuthorization(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAuthorization(opt =>
        {
            opt.AddPolicy(AdminPolicy, p =>
            {
                p.RequireAuthenticatedUser();
                p.RequireRole("Admin"); // claim "perfil" == "Admin" (PascalCase) emitido pelo JwtAccessTokenService.
            });
        });

        return services;
    }
}
