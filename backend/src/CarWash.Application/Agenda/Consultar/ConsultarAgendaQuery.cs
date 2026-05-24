using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Agenda.Common;

namespace CarWash.Application.Agenda.Consultar;

/// <summary>
/// Consulta somente-leitura da agenda (RF009). Os parâmetros chegam crus
/// (<see cref="string"/>) do endpoint para que o <c>ConsultarAgendaQueryValidator</c>
/// controle as mensagens de 400 por campo — o binder não-nullable produziria
/// mensagens genéricas. O handler reparseia após o validator garantir o parse.
/// </summary>
public sealed record ConsultarAgendaQuery(
    string? Formato,
    string? Inicio,
    string? Fim,
    string? FilialId,
    string? ClienteId,
    string? UsuarioId,
    string? Status,
    string TraceId) : IQuery<ConsultarAgendaResponse>;
