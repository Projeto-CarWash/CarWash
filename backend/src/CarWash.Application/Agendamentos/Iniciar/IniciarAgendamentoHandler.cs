using System.Text.Json;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Agendamentos.Common;
using CarWash.Application.Agendamentos.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Domain.Common;
using CarWash.Domain.Entities;
using CarWash.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CarWash.Application.Agendamentos.Iniciar;

/// <summary>
/// Use case de início de atendimento (AGENDADO → EM_ANDAMENTO). Valida o
/// status atual (apenas AGENDADO pode ser iniciado; demais → 409), persiste
/// com auditoria e histórico (evento INICIADO — RN007). Enquanto em
/// andamento, o agendamento continua ocupando célula da filial (RF008) e a
/// janela do veículo (RN011).
/// </summary>
public sealed class IniciarAgendamentoHandler
    : ICommandHandler<IniciarAgendamentoCommand, IniciarAgendamentoResponse>
{
    private readonly IAgendamentoRepository _agendamentos;
    private readonly ILogger<IniciarAgendamentoHandler> _logger;

    public IniciarAgendamentoHandler(
        IAgendamentoRepository agendamentos,
        ILogger<IniciarAgendamentoHandler> logger)
    {
        _agendamentos = agendamentos;
        _logger = logger;
    }

    public async Task<IniciarAgendamentoResponse> HandleAsync(
        IniciarAgendamentoCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var usuarioId = command.UsuarioId ?? throw new ValidationException(
            "Não foi possível identificar o usuário autenticado.",
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["usuario"] = ["Usuário autenticado é obrigatório."],
            });

        var agendamento = await _agendamentos.ObterPorIdRastreadoAsync(
            command.AgendamentoId, cancellationToken).ConfigureAwait(false);

        if (agendamento is null)
        {
            throw new NotFoundException("Agendamento não encontrado.");
        }

        var statusAnterior = agendamento.Status;

        try
        {
            agendamento.Iniciar();
        }
        catch (DomainException ex)
        {
            // Transição inválida de estado → 409 (mesmo padrão do cancelamento).
            throw new TransicaoStatusException(ex.Message);
        }

        var historico = AgendamentoHistorico.Registrar(
            id: Guid.NewGuid(),
            agendamentoId: agendamento.Id,
            evento: EventoHistorico.Iniciado,
            usuarioId: usuarioId,
            payload: JsonSerializer.Serialize(new
            {
                statusAnterior = statusAnterior.ToDbValue(),
                statusNovo = agendamento.Status.ToDbValue(),
            }));

        await _agendamentos.SalvarAsync(
            agendamento,
            historico,
            command.TraceId,
            "AGENDAMENTO_INICIADO",
            usuarioId,
            JsonSerializer.Serialize(new
            {
                agendamento.Id,
                StatusNovo = agendamento.StatusRaw,
            }),
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Atendimento iniciado. AgendamentoId: {AgendamentoId}. UsuarioId: {UsuarioId}. TraceId: {TraceId}",
            agendamento.Id,
            usuarioId,
            command.TraceId);

        return new IniciarAgendamentoResponse
        {
            Message = "Atendimento iniciado com sucesso.",
            Data = new IniciarAgendamentoData
            {
                Id = agendamento.Id,
                Status = agendamento.Status.ToDbValue(),
                AtualizadoEm = agendamento.AtualizadoEm,
            },
            TraceId = command.TraceId,
        };
    }
}
