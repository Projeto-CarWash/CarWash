using CarWash.Application.Services.Clientes;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CarWash.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IClienteService, ClienteService>();

        return services;
    }
}
