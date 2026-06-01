using CarWash.Api.Endpoints.Agenda;
using CarWash.Api.Endpoints.Agendamentos;
using CarWash.Api.Endpoints.Auth;
using CarWash.Api.Endpoints.Clientes;
using CarWash.Api.Endpoints.Filiais;
using CarWash.Api.Endpoints.Servicos;
using CarWash.Api.Endpoints.Usuarios;
using CarWash.Api.Endpoints.Veiculos;

namespace CarWash.Api.Endpoints;

/// <summary>
/// Ponto único de registro dos endpoints CarWash. <c>Program.cs</c> chama
/// <see cref="MapCarWashEndpoints"/> uma vez para manter a configuração enxuta.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapCarWashEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.MapUsuarios();
        app.MapAuth();
        app.MapClientes();
        app.MapFiliais();
        app.MapVeiculos();
        app.MapAgendamentos();
        app.MapServicos();
        app.MapAgenda();
        app.MapAgendamentoObservacoes();
        app.MapClienteHistoricoAtendimentos();
        return app;
    }
}
