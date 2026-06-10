namespace CarWash.Application.Agenda.Common;

/// <summary>
/// Envelope padronizado da consulta de agenda (RF009). <see cref="Data"/> carrega
/// itens de <see cref="AgendaItemSimplesResponse"/> ou
/// <see cref="AgendaItemDetalhadoResponse"/> conforme o formato pedido — o tipo
/// efetivo é resolvido em tempo de execução pelo handler.
/// </summary>
public sealed class ConsultarAgendaResponse
{
    /// <summary>Gets mensagem amigável de status da consulta.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets eventos da agenda. Cada item é <see cref="AgendaItemSimplesResponse"/>
    /// (formato simples) ou <see cref="AgendaItemDetalhadoResponse"/> (detalhado).
    /// </summary>
    public IReadOnlyList<object> Data { get; init; } = [];

    /// <summary>Gets identificador de correlação da requisição.</summary>
    public string TraceId { get; init; } = string.Empty;
}
