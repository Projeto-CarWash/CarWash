namespace CarWash.Application.Usuarios.AlterarStatus;

/// <summary>
/// Resposta do <c>PATCH /api/v1/usuarios/{id}/status</c>. Retorna estado atual
/// (mesmo em no-op) para o cliente confirmar a operação.
/// </summary>
public sealed record AlterarStatusUsuarioResponse(Guid Id, bool Ativo, DateTime AtualizadoEm);
