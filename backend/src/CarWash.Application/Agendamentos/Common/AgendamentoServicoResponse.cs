namespace CarWash.Application.Agendamentos.Common;

/// <summary>
/// Serviço incluído no agendamento, com preço e duração congelados no momento
/// da criação (snapshot — RN006: o catálogo pode mudar depois sem afetar a agenda).
/// </summary>
public sealed class AgendamentoServicoResponse
{
    public Guid Id { get; init; }

    public Guid ServicoId { get; init; }

    public string NomeServico { get; init; } = string.Empty;

    public decimal PrecoAplicado { get; init; }

    public int DuracaoAplicada { get; init; }
}
