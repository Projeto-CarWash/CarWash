using System.Text.Json;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Agendamentos.Common;
using CarWash.Application.Agendamentos.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Domain.Entities;
using CarWash.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CarWash.Application.Agendamentos.Editar;

/// <summary>
/// Use case de edição de agendamento (RF010). Valida o status atual
/// (apenas AGENDADO permite edição; FINALIZADO/CANCELADO/EM_ANDAMENTO→409),
/// aplica as alterações parciais, persiste com auditoria e histórico.
/// Concorrência otimista: se o status mudou entre o lookup e o commit,
/// o EF lança <c>DbUpdateConcurrencyException</c> → o handler traduz em 409.
/// </summary>
public sealed class EditarAgendamentoHandler
    : ICommandHandler<EditarAgendamentoCommand, EditarAgendamentoResponse>
{
    private readonly IAgendamentoRepository _agendamentos;
    private readonly ILogger<EditarAgendamentoHandler> _logger;

    public EditarAgendamentoHandler(
        IAgendamentoRepository agendamentos,
        ILogger<EditarAgendamentoHandler> logger)
    {
        _agendamentos = agendamentos;
        _logger = logger;
    }

    public async Task<EditarAgendamentoResponse> HandleAsync(
        EditarAgendamentoCommand command, CancellationToken cancellationToken)
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

        GarantirStatusEditavel(statusAnterior);

        AplicarAlteracoes(agendamento, command);

        var historico = AgendamentoHistorico.Registrar(
            id: Guid.NewGuid(),
            agendamentoId: agendamento.Id,
            evento: EventoHistorico.Editado,
            usuarioId: usuarioId,
            payload: JsonSerializer.Serialize(new
            {
                statusAnterior = statusAnterior.ToDbValue(),
                statusNovo = agendamento.Status.ToDbValue(),
                inicio = command.Inicio,
                fim = command.Fim,
                responsavelId = command.ResponsavelId,
                observacoes = command.Observacoes,
            }));

        await _agendamentos.SalvarAsync(
            agendamento,
            historico,
            command.TraceId,
            "AGENDAMENTO_EDITADO",
            usuarioId,
            JsonSerializer.Serialize(new
            {
                agendamento.Id,
                StatusNovo = agendamento.StatusRaw,
                command.Inicio,
                command.Fim,
                command.ResponsavelId,
                command.Observacoes,
            }),
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Agendamento editado. AgendamentoId: {AgendamentoId}. Status: {Status}. " +
            "UsuarioId: {UsuarioId}. TraceId: {TraceId}",
            agendamento.Id,
            agendamento.Status.ToDbValue(),
            usuarioId,
            command.TraceId);

        return new EditarAgendamentoResponse
        {
            Message = "Agendamento atualizado com sucesso.",
            Data = new EditarAgendamentoData
            {
                Id = agendamento.Id,
                Status = agendamento.Status.ToDbValue(),
                AtualizadoEm = agendamento.AtualizadoEm,
            },
            TraceId = command.TraceId,
        };
    }

    private static void GarantirStatusEditavel(StatusAgendamento statusAnterior)
    {
        if (statusAnterior is not StatusAgendamento.Agendado)
        {
            throw statusAnterior switch
            {
                StatusAgendamento.Finalizado => new EdicaoBloqueadaException(
                    EdicaoBloqueadaException.MensagemFinalizado, statusAnterior.ToDbValue()),
                StatusAgendamento.Cancelado => new EdicaoBloqueadaException(
                    EdicaoBloqueadaException.MensagemCancelado, statusAnterior.ToDbValue()),
                StatusAgendamento.EmAndamento => new EdicaoBloqueadaException(
                    EdicaoBloqueadaException.MensagemEmAndamento, statusAnterior.ToDbValue()),
                _ => new EdicaoBloqueadaException(
                    $"Não é possível editar um agendamento com status {statusAnterior.ToDbValue()}.", statusAnterior.ToDbValue()),
            };
        }
    }

    private static void AplicarAlteracoes(
        Agendamento agendamento, EditarAgendamentoCommand command)
    {
        if (command.Inicio.HasValue || command.Fim.HasValue)
        {
            var novoInicio = command.Inicio ?? agendamento.Inicio;
            var novoFim = command.Fim ?? agendamento.Fim;
            agendamento.Reagendar(novoInicio, novoFim);
        }

        if (command.ResponsavelId.HasValue)
        {
            agendamento.DefinirResponsavel(command.ResponsavelId.Value);
        }

        if (command.Observacoes is not null)
        {
            agendamento.AlterarObservacoes(command.Observacoes);
        }
    }
}
