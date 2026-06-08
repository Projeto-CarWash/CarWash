using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Usuarios.Preferencias.Common;
using CarWash.Application.Usuarios.Preferencias.Persistence;
using Microsoft.Extensions.Logging;

namespace CarWash.Application.Usuarios.Preferencias.Consultar;

public sealed class ConsultarMinhasPreferenciasHandler
    : IQueryHandler<ConsultarMinhasPreferenciasQuery, UsuarioPreferenciasResponse>
{
    private readonly IUsuarioPreferenciaRepository _repository;
    private readonly ILogger<ConsultarMinhasPreferenciasHandler> _logger;

    public ConsultarMinhasPreferenciasHandler(
        IUsuarioPreferenciaRepository repository,
        ILogger<ConsultarMinhasPreferenciasHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<UsuarioPreferenciasResponse> HandleAsync(
        ConsultarMinhasPreferenciasQuery query,
        CancellationToken cancellationToken)
    {
        var preferencia = await _repository
            .ObterPorUsuarioIdAsync(query.UsuarioId, cancellationToken)
            .ConfigureAwait(false);

        string theme = preferencia?.TemaRaw ?? "light";

        _logger.LogInformation(
            "Preferência de tema consultada. TraceId={TraceId}, UsuarioId={UsuarioId}, Theme={Theme}",
            query.TraceId,
            query.UsuarioId,
            theme);

        return new UsuarioPreferenciasResponse
        {
            Message = "Preferência de tema consultada com sucesso.",
            Data = new UsuarioPreferenciasDataResponse
            {
                Theme = theme,
            },
            TraceId = query.TraceId,
        };
    }
}
