using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Auth.Login;
using CarWash.Application.Usuarios.AlterarStatus;
using CarWash.Application.Usuarios.Common;
using CarWash.Application.Usuarios.CriarUsuario;
using CarWash.Application.Usuarios.ObterUsuarioPorId;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CarWash.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        services.AddScoped<ICommandHandler<CriarUsuarioCommand, UsuarioResponse>, CriarUsuarioHandler>();
        services.AddScoped<IQueryHandler<ObterUsuarioPorIdQuery, UsuarioResponse>, ObterUsuarioPorIdHandler>();
        services.AddScoped<ICommandHandler<AlterarStatusUsuarioCommand, AlterarStatusUsuarioResponse>, AlterarStatusUsuarioHandler>();
        services.AddScoped<ICommandHandler<LoginCommand, LoginResponse>, LoginHandler>();

        return services;
    }
}
