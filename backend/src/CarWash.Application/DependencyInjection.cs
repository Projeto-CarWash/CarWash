using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Auth.Login;
using CarWash.Application.Auth.Logout;
using CarWash.Application.Auth.Refresh;
using CarWash.Application.Clientes.AlterarStatus;
using CarWash.Application.Clientes.Atualizar;
using CarWash.Application.Clientes.Common;
using CarWash.Application.Clientes.Criar;
using CarWash.Application.Clientes.Listar;
using CarWash.Application.Clientes.ObterPorId;
using CarWash.Application.Usuarios.AlterarStatus;
using CarWash.Application.Usuarios.AlterarUsuario;
using CarWash.Application.Usuarios.Common;
using CarWash.Application.Usuarios.CriarUsuario;
using CarWash.Application.Usuarios.Listar;
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

        // Usuarios
        services.AddScoped<ICommandHandler<CriarUsuarioCommand, UsuarioResponse>, CriarUsuarioHandler>();
        services.AddScoped<IQueryHandler<ObterUsuarioPorIdQuery, UsuarioResponse>, ObterUsuarioPorIdHandler>();
        services.AddScoped<IQueryHandler<ListarUsuariosQuery, ListaUsuariosResponse>, ListarUsuariosHandler>();
        services.AddScoped<ICommandHandler<AlterarStatusUsuarioCommand, AlterarStatusUsuarioResponse>, AlterarStatusUsuarioHandler>();
        services.AddScoped<ICommandHandler<AlterarUsuarioCommand, UsuarioResponse>, AlterarUsuarioHandler>();

        // Auth
        services.AddScoped<ICommandHandler<LoginCommand, LoginResultado>, LoginHandler>();
        services.AddScoped<ICommandHandler<RefreshCommand, RefreshResultado>, RefreshHandler>();
        services.AddScoped<ICommandHandler<LogoutCommand, LogoutResultado>, LogoutHandler>();

        // Clientes
        services.AddScoped<ICommandHandler<CriarClienteCommand, CriarClienteResponse>, CriarClienteHandler>();
        services.AddScoped<ICommandHandler<AtualizarClienteCommand, ClienteResponse>, AtualizarClienteHandler>();
        services.AddScoped<ICommandHandler<AlterarStatusClienteCommand, ClienteResponse>, AlterarStatusClienteHandler>();
        services.AddScoped<IQueryHandler<ObterClientePorIdQuery, ClienteResponse>, ObterClientePorIdHandler>();
        services.AddScoped<IQueryHandler<ListarClientesQuery, ListaClientesResponse>, ListarClientesHandler>();

        return services;
    }
}
