namespace CarWash.Application.Filiais.AlterarCelulasAtivas;

/// <summary>
/// DTO de entrada do <c>PATCH /api/v1/filiais/{id}/celulas-ativas</c>.
/// <c>CelulasAtivas</c> é nullable para diferenciar "ausente" de <c>0</c> — o
/// validator exige <c>NotNull</c> para rejeitar body vazio <c>{}</c> com a
/// mensagem específica do card (faixa 1..100). Mesmo padrão de
/// <see cref="Usuarios.AlterarStatus.AlterarStatusUsuarioRequest"/> (BUG-U004).
/// </summary>
public sealed record AlterarCelulasAtivasRequest(int? CelulasAtivas);
