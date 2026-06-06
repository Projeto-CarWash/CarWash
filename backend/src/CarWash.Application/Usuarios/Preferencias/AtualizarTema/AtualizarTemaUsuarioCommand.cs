using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Usuarios.Preferencias.Common;

namespace CarWash.Application.Usuarios.Preferencias.AtualizarTema;

public sealed record AtualizarTemaUsuarioCommand(
    Guid UsuarioId,
    string? Theme,
    string TraceId) : ICommand<UsuarioPreferenciasResponse>;
