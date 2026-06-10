namespace CarWash.Application.Agendamentos.Editar;

/// <summary>
/// DTO de entrada do <c>PATCH /api/v1/agendamentos/{id}</c> (RF010).
/// Todos os campos são opcionais — apenas os enviados serão alterados.
/// </summary>
public sealed class EditarAgendamentoRequest
{
    public DateTime? Inicio { get; set; }

    public DateTime? Fim { get; set; }

    public Guid? ResponsavelId { get; set; }

    public string? Observacoes { get; set; }
}
