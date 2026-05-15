using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Usuarios.Common;

namespace CarWash.Application.Usuarios.ObterUsuarioPorId;

/// <summary>
/// Consulta por id. Retorna <see cref="UsuarioResponse"/> sem <c>SenhaHash</c>.
/// </summary>
public sealed record ObterUsuarioPorIdQuery(Guid Id) : IQuery<UsuarioResponse>;
