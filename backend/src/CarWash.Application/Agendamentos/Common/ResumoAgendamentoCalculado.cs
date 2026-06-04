using CarWash.Application.Agendamentos.Persistence;

namespace CarWash.Application.Agendamentos.Common;

/// <summary>
/// Resultado do <see cref="CalculadoraResumoAgendamento"/>: agrega o resumo
/// pronto para a resposta HTTP, os snapshots dos serviços (com preço/duração de
/// catálogo, ordenados conforme o pedido do cliente), o snapshot do responsável
/// (RF024) e os dados normalizados que os handlers de criação/confirmação
/// reutilizam para montar o agregado de domínio.
/// </summary>
public sealed record ResumoAgendamentoCalculado(
    ResumoConfirmacaoResponse Resumo,
    IReadOnlyList<ServicoSnapshot> Servicos,
    DateTime Inicio,
    DateTime Fim,
    int DuracaoTotalMin,
    decimal ValorTotal,
    string? Observacoes,
    ResponsavelResumoSnapshot Responsavel)
{
    /// <summary>Atalho para o <c>hashResumo</c> calculado.</summary>
    public string HashResumo => Resumo.HashResumo;
}
