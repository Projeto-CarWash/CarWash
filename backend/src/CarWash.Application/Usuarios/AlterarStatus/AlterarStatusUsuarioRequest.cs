namespace CarWash.Application.Usuarios.AlterarStatus;

/// <summary>
/// DTO de entrada do endpoint <c>PATCH /api/v1/usuarios/{id}/status</c>.
/// O <c>id</c> vem da rota; o body contém apenas <c>ativo</c>.
/// <para>
/// <c>Ativo</c> é nullable para que o body <c>{}</c> (campo ausente) seja capturado
/// como erro de validação em vez de cair silenciosamente em <c>false</c>
/// (BUG-U004 — desativação silenciosa do alvo).
/// </para>
/// </summary>
public sealed record AlterarStatusUsuarioRequest(bool? Ativo);
