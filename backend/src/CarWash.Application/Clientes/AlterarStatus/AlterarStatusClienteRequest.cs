namespace CarWash.Application.Clientes.AlterarStatus;

/// <summary>
/// DTO de entrada do <c>PATCH /api/v1/clientes/{id}/status</c>.
/// <para>
/// <c>Ativo</c> é nullable para que o body <c>{}</c> (campo ausente) seja capturado
/// como erro de validação em vez de cair silenciosamente em <c>false</c>
/// (GAP-CW-CLI-STA-EMP).
/// </para>
/// </summary>
public sealed record AlterarStatusClienteRequest(bool? Ativo);
