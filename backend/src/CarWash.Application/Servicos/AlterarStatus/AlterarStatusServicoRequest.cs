namespace CarWash.Application.Servicos.AlterarStatus;

/// <summary>
/// DTO de entrada do <c>PATCH /api/v1/servicos/{id}/status</c>.
/// <para>
/// <c>Ativo</c> é nullable para que o body <c>{}</c> (campo ausente) seja capturado
/// como erro de validação em vez de cair silenciosamente em <c>false</c>.
/// </para>
/// </summary>
public sealed record AlterarStatusServicoRequest(bool? Ativo);
