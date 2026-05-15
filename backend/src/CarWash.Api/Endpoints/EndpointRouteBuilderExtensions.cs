using CarWash.Api.Endpoints.Auth;
using CarWash.Api.Endpoints.Usuarios;

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
        return app;
    }
}
