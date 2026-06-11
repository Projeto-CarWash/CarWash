using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Agendamentos.Persistence;
using CarWash.Application.Common.Exceptions;

namespace CarWash.Application.Agendamentos.ObterPorId;

public sealed class ObterAgendamentoPorIdHandler : IQueryHandler<ObterAgendamentoPorIdQuery, ObterAgendamentoPorIdResponse>
{
    public const string MensagemNaoEncontrado = "Agendamento não encontrado.";

    private readonly IAgendamentoRepository _repositorio;

    public ObterAgendamentoPorIdHandler(IAgendamentoRepository repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<ObterAgendamentoPorIdResponse> HandleAsync(
        ObterAgendamentoPorIdQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var agendamento = await _repositorio.ObterPorIdAsync(query.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(MensagemNaoEncontrado);

        return new ObterAgendamentoPorIdResponse
        {
            Data = ObterAgendamentoPorIdData.FromEntity(agendamento),
using System.Text.Json;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Agendamentos.Common;
using CarWash.Application.Agendamentos.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Domain.Entities;
using CarWash.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CarWash.Application.Agendamentos.ObterPorId;

/// <summary>
/// Use case de consulta detalhada de agendamento por id (RF010).
/// Carrega o agregado com itens e monta o <see cref="AgendamentoDetalhadoResponse"/>
/// com todos os campos e mascaramento de dados sensíveis.
/// </summary>
public sealed class ObterAgendamentoPorIdHandler
    : IQueryHandler<ObterAgendamentoPorIdQuery, AgendamentoDetalhadoResponse>
{
    public const string MensagemNaoEncontrado = "Agendamento não encontrado.";

    private readonly IAgendamentoRepository _agendamentos;
    private readonly IAgendamentoCatalogoRepository _catalogo;
    private readonly ILogger<ObterAgendamentoPorIdHandler> _logger;

    public ObterAgendamentoPorIdHandler(
        IAgendamentoRepository agendamentos,
        IAgendamentoCatalogoRepository catalogo,
        ILogger<ObterAgendamentoPorIdHandler> logger)
    {
        _agendamentos = agendamentos;
        _catalogo = catalogo;
        _logger = logger;
    }

    public async Task<AgendamentoDetalhadoResponse> HandleAsync(
        ObterAgendamentoPorIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var resultado = await _agendamentos.ObterPorIdComItensAsync(
            query.Id, cancellationToken).ConfigureAwait(false);

        if (resultado is null)
        {
            throw new NotFoundException(MensagemNaoEncontrado);
        }

        var (agendamento, itens) = resultado.Value;

        var servicoIds = itens.Select(i => i.ServicoId).Distinct().ToArray();
        var servicos = await _catalogo.ObterServicosAsync(
            servicoIds, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Agendamento consultado por id. TraceId: {TraceId}. AgendamentoId: {AgendamentoId}",
            query.TraceId,
            agendamento.Id);

        return MontarResponse(agendamento, itens, servicos, query.TraceId);
    }

    private static AgendamentoDetalhadoResponse MontarResponse(
        Agendamento agendamento,
        IReadOnlyCollection<AgendamentoItem> itens,
        IReadOnlyList<ServicoSnapshot> servicos,
        string traceId)
    {
        return new AgendamentoDetalhadoResponse
        {
            Message = "Agendamento encontrado.",
            Data = new AgendamentoDetalhadoData
            {
                Id = agendamento.Id,
                FilialId = agendamento.FilialId,
                ClienteId = agendamento.ClienteId,
                VeiculoId = agendamento.VeiculoId,
                ResponsavelId = agendamento.ResponsavelId,
                Status = agendamento.Status.ToDbValue(),
                Inicio = agendamento.Inicio,
                Fim = agendamento.Fim,
                DuracaoTotalMin = agendamento.DuracaoTotalMin,
                ValorTotal = agendamento.ValorTotal,
                Observacoes = agendamento.Observacoes,
                Versao = agendamento.Versao,
                CriadoEm = agendamento.CriadoEm,
                AtualizadoEm = agendamento.AtualizadoEm,
                CanceladoEm = agendamento.CanceladoEm,
                CanceladoPor = agendamento.CanceladoPor,
                MotivoCancelamento = agendamento.MotivoCancelamento,
                CriadoPor = agendamento.CriadoPor,
                Itens = itens
                    .Select(item => new AgendamentoServicoResponse
                    {
                        Id = item.Id,
                        ServicoId = item.ServicoId,
                        NomeServico = servicos.FirstOrDefault(s => s.Id == item.ServicoId)?.Nome ?? "Serviço removido",
                        PrecoAplicado = item.PrecoAplicado,
                        DuracaoAplicada = item.DuracaoAplicada,
                    })
                    .ToList(),
            },
            TraceId = traceId,
        };
    }
}
