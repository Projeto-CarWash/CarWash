using System.Reflection;
using CarWash.Application.Abstractions.Messaging;
using FluentValidation;

namespace CarWash.Api.Filters;

/// <summary>
/// Registra <see cref="ValidationFilter{T}"/> fechado para todo tipo no assembly
/// da Application que implemente <see cref="ICommand{T}"/> ou <see cref="IQuery{T}"/>
/// e tenha um <see cref="IValidator{T}"/> conhecido (FluentValidation já registrado
/// via <c>AddValidatorsFromAssembly</c>). Substitui a configuração manual em
/// <c>Program.cs</c> e elimina o risco de "filter esquecido" ao adicionar um
/// command novo.
/// </summary>
public static class ValidationFilterRegistration
{
    public static IServiceCollection AddValidationFilters(this IServiceCollection services, Assembly applicationAssembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(applicationAssembly);

        var validatorBase = typeof(IValidator<>);
        var filtroBase = typeof(ValidationFilter<>);

        // Set com os tipos validados pelo FluentValidation no assembly.
        var alvosValidados = applicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .SelectMany(t => t.GetInterfaces())
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == validatorBase)
            .Select(i => i.GetGenericArguments()[0])
            .ToHashSet();

        // Para cada Command<T>/Query<T> que tem validador, registra o filtro fechado.
        var marcadores = new[] { typeof(ICommand<>), typeof(IQuery<>) };
        foreach (var alvo in alvosValidados)
        {
            bool implementaMarcador = alvo.GetInterfaces()
                .Any(i => i.IsGenericType && marcadores.Contains(i.GetGenericTypeDefinition()));
            if (!implementaMarcador)
            {
                continue;
            }

            var filtroFechado = filtroBase.MakeGenericType(alvo);
            services.AddScoped(filtroFechado);
        }

        return services;
    }
}
