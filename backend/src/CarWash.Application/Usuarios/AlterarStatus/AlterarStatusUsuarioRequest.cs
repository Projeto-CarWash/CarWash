namespace CarWash.Application.Usuarios.AlterarStatus;

/// <summary>
/// DTO de entrada do endpoint <c>PATCH /api/v1/usuarios/{id}/status</c>.
/// O <c>id</c> vem da rota; o body contém apenas <c>ativo</c>.
/// </summary>
public sealed record AlterarStatusUsuarioRequest(bool Ativo);
