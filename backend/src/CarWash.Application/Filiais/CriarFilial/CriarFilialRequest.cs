namespace CarWash.Application.Filiais.CriarFilial;

/// <summary>
/// DTO de entrada do <c>POST /api/v1/filiais</c>. <c>CelulasAtivas</c> é nullable
/// para distinguir "ausente no body" de <c>0</c> — o validator exige <c>NotNull</c>
/// (mesmo padrão do <see cref="Usuarios.AlterarStatus.AlterarStatusUsuarioRequest"/>,
/// BUG-U004).
/// </summary>
public sealed record CriarFilialRequest(string? Nome, int? CelulasAtivas, string? Timezone);
