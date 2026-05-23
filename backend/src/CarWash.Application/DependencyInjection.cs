using System.Reflection;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Agendamentos.Common;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CarWash.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var assembly = typeof(DependencyInjection).Assembly;

        // Validators do FluentValidation — escaneia todo o assembly.
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // Handlers CQRS — escaneia tipos concretos que implementam
        // ICommandHandler<,> ou IQueryHandler<,> e registra como Scoped por
        // interface fechada. Substitui o registro manual por slice e evita
        // que um handler novo seja esquecido na bandeja do DI.
        RegistrarHandlers(services, assembly, typeof(ICommandHandler<,>));
        RegistrarHandlers(services, assembly, typeof(IQueryHandler<,>));

        // Serviço de domínio de cálculo do resumo de agendamento (RF007/RF015) —
        // não é handler nem validator, então não é alcançado pelo scan acima.
        services.AddScoped<CalculadoraResumoAgendamento>();

        return services;
    }

    private static void RegistrarHandlers(IServiceCollection services, Assembly assembly, Type interfaceAberta)
    {
        var tipos = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == interfaceAberta)
                .Select(i => (Implementacao: t, Interface: i)));

        foreach (var (implementacao, interfaceFechada) in tipos)
        {
            services.AddScoped(interfaceFechada, implementacao);
        }
    }
}
