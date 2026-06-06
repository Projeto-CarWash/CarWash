using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Usuarios.Preferencias.Common;

namespace CarWash.Application.Usuarios.Preferencias.Consultar;

public sealed record ConsultarMinhasPreferenciasQuery(
    Guid UsuarioId,
    string TraceId) : IQuery<UsuarioPreferenciasResponse>;
