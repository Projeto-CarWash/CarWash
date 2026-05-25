using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Agendamentos.Common;

namespace CarWash.Application.Agendamentos.Criar;

/// <summary>
/// Comando de criação de agendamento (RF007/RF019/RF020/RF024). A janela é
/// derivada de <c>Inicio</c> + soma das durações dos serviços. <c>TraceId</c> e
/// <c>UsuarioId</c> são preenchidos pelo endpoint a partir do <c>HttpContext</c>.
/// </summary>
public sealed record CriarAgendamentoCommand(
    Guid FilialId,
    Guid ClienteId,
    Guid VeiculoId,
    Guid? ResponsavelId,
    DateTime? Inicio,
    IReadOnlyList<Guid>? ServicoIds,
    string? Observacoes,
    string TraceId,
    Guid? UsuarioId) : ICommand<AgendamentoResponse>;
