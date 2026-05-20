using CarWash.Application.Interfaces;
using CarWash.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CarWash.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IVeiculoService, VeiculoService>();

        return services;
    }
}
