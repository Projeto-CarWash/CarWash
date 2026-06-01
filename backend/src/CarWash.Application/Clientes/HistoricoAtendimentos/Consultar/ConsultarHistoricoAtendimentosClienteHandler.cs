using System.Diagnostics;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Clientes.HistoricoAtendimentos.Common;
using CarWash.Application.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace CarWash.Application.Clientes.HistoricoAtendimentos.Consultar;

public sealed class ConsultarHistoricoAtendimentosClienteHandler
    : IQueryHandler<ConsultarHistoricoAtendimentosClienteQuery, HistoricoAtendimentosResponse>
{
    private readonly IHistoricoAtendimentosClienteRepository repository;
    private readonly IValidator<ConsultarHistoricoAtendimentosClienteQuery> validator;
    private readonly ILogger<ConsultarHistoricoAtendimentosClienteHandler> logger;

    public ConsultarHistoricoAtendimentosClienteHandler(
        IHistoricoAtendimentosClienteRepository repository,
        IValidator<ConsultarHistoricoAtendimentosClienteQuery> validator,
        ILogger<ConsultarHistoricoAtendimentosClienteHandler> logger)
    {
        this.repository = repository;
        this.validator = validator;
        this.logger = logger;
    }

    public async Task<HistoricoAtendimentosResponse> HandleAsync(
        ConsultarHistoricoAtendimentosClienteQuery query,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        bool clienteExiste = await repository.ClienteExisteAsync(
            query.ClienteId,
            cancellationToken);

        if (!clienteExiste)
        {
            throw new ClienteHistoricoNaoEncontradoException();
        }

        var resultado = await repository.ConsultarAsync(
            query.ClienteId,
            query.DataInicio,
            query.DataFim,
            query.UltimosDias,
            query.Status,
            query.Page,
            query.PageSize,
            cancellationToken);

        stopwatch.Stop();

        logger.LogInformation(
            "Histórico de atendimentos consultado. TraceId: {TraceId}. ClienteId: {ClienteId}. Status: {Status}. DataInicio: {DataInicio}. DataFim: {DataFim}. UltimosDias: {UltimosDias}. QuantidadeRetornada: {QuantidadeRetornada}. Total: {Total}. TempoMs: {TempoMs}",
            query.TraceId,
            query.ClienteId,
            query.Status,
            query.DataInicio,
            query.DataFim,
            query.UltimosDias,
            resultado.Itens.Count,
            resultado.Total,
            stopwatch.ElapsedMilliseconds);

        return new HistoricoAtendimentosResponse
        {
            Message = resultado.Total == 0
                ? "Nenhum atendimento encontrado para este cliente."
                : "Histórico de atendimentos consultado com sucesso.",
            Data = resultado.Itens.ToList(),
            Meta = new HistoricoAtendimentosMetaResponse
            {
                Total = resultado.Total,
                Page = query.Page,
                PageSize = query.PageSize,
            },
            TraceId = query.TraceId,
        };
    }
}
