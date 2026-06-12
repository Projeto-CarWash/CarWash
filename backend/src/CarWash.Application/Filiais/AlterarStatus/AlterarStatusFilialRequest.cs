namespace CarWash.Application.Filiais.AlterarStatus;

/// <summary>
/// DTO de entrada do endpoint <c>PATCH /api/v1/filiais/{id}/status</c>
/// (RF017/RF019). O <c>id</c> vem da rota; o body contém apenas <c>ativo</c>.
/// <para>
/// <c>Ativo</c> é nullable para que o body <c>{}</c> (campo ausente) seja
/// capturado como erro de validação em vez de cair silenciosamente em
/// <c>false</c> (mesmo racional do BUG-U004 em usuários).
/// </para>
/// </summary>
public sealed record AlterarStatusFilialRequest(bool? Ativo);
