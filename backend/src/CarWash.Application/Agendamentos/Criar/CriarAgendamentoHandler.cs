using CarWash.Application.Abstractions;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Agendamentos.Common;
using CarWash.Application.Agendamentos.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Domain.Entities;
using CarWash.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CarWash.Application.Agendamentos.Criar;

public sealed class CriarAgendamentoHandler : ICommandHandler<CriarAgendamentoCommand, CriarAgendamentoResponse>
{
    private readonly IAgendamentoRepository _repositorio;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<CriarAgendamentoHandler> _logger;

    public CriarAgendamentoHandler(
        IAgendamentoRepository repositorio,
        IAuditLogger auditLogger,
        ILogger<CriarAgendamentoHandler> logger)
    {
        _repositorio = repositorio;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task<CriarAgendamentoResponse> HandleAsync(
        CriarAgendamentoCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var filial = await _repositorio.ObterFilialPorIdAsync(command.FilialId, cancellationToken);
        if (filial is null || !filial.Ativa)
        {
            throw new NotFoundException("Filial não encontrada ou inativa.");
        }

        if (filial.CelulasAtivas < 1)
        {
            throw new NotFoundException("Filial não encontrada ou inativa.");
        }

        var cliente = await _repositorio.ObterClientePorIdAsync(command.ClienteId, cancellationToken);
        if (cliente is null || !cliente.Ativo)
        {
            throw new NotFoundException("Cliente não encontrado ou inativo.");
        }

        var veiculo = await _repositorio.ObterVeiculoPorIdAsync(command.VeiculoId, cancellationToken);
        if (veiculo is null || !veiculo.Ativo)
        {
            throw new NotFoundException("Veículo não encontrado ou inativo.");
        }

        if (veiculo.ClienteId != command.ClienteId)
        {
            throw new ValidationException(
                "Dados do agendamento inválidos. Verifique os campos e tente novamente.",
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["veiculoId"] = ["Veículo não pertence ao cliente informado."],
                });
        }

        if (command.ResponsavelId.HasValue)
        {
            var filiado = await _repositorio.ObterFiliadoPorIdAsync(
                command.ResponsavelId.Value, cancellationToken);
            if (filiado is null || !filiado.Ativo)
            {
                throw new NotFoundException("Responsável informado não foi encontrado.");
            }

            if (filiado.ClienteId != command.ClienteId)
            {
                throw new ResponsavelConflitoException();
            }
        }

        var servicos = await _repositorio.ObterServicosPorIdsAsync(
            command.ServicoIds, cancellationToken);

        if (servicos.Count != command.ServicoIds.Count)
        {
            throw new ValidationException(
                "Dados do agendamento inválidos. Verifique os campos e tente novamente.",
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["servicoIds"] = ["Um ou mais serviços não foram encontrados ou estão inativos."],
                });
        }

        foreach (var servico in servicos)
        {
            if (!servico.Ativo)
            {
                throw new ValidationException(
                    "Dados do agendamento inválidos. Verifique os campos e tente novamente.",
                    new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["servicoIds"] = ["Um ou mais serviços não foram encontrados ou estão inativos."],
                    });
            }
        }

        var duracaoTotalMin = servicos.Sum(s => s.DuracaoMin);
        var valorTotal = servicos.Sum(s => s.Preco);
        var inicio = DateTime.SpecifyKind(command.Inicio, DateTimeKind.Utc);
        var fim = inicio.AddMinutes(duracaoTotalMin);

        var conflitoVeiculo = await _repositorio.ExisteConflitoVeiculoAsync(
            command.VeiculoId, inicio, fim, cancellationToken);

        if (conflitoVeiculo)
        {
            _logger.LogWarning(
                "Conflito de veículo. TraceId: {TraceId}, FilialId: {FilialId}, VeiculoId: {VeiculoId}, "
                + "Inicio: {Inicio}, Fim: {Fim}, Motivo: {Motivo}",
                command.TraceId, command.FilialId, command.VeiculoId, inicio, fim, "veiculo");

            await _auditLogger.LogAsync(
                evento: "AGENDAMENTO_REJEITADO",
                entidade: "agendamentos",
                entidadeId: null,
                dados: new
                {
                    motivo = "veiculo",
                    filialId = command.FilialId,
                    veiculoId = command.VeiculoId,
                    clienteId = command.ClienteId,
                    inicio,
                    fim,
                },
                cancellationToken: cancellationToken).ConfigureAwait(false);

            throw new VeiculoConflitoException();
        }

        var ocupacaoAtual = await _repositorio.ContarOcupacaoAsync(
            command.FilialId, inicio, fim, cancellationToken);

        if (ocupacaoAtual >= filial.CelulasAtivas)
        {
            _logger.LogWarning(
                "Capacidade da filial atingida. TraceId: {TraceId}, FilialId: {FilialId}, "
                + "VeiculoId: {VeiculoId}, Inicio: {Inicio}, Fim: {Fim}, "
                + "OcupacaoAtual: {OcupacaoAtual}, CelulasAtivas: {CelulasAtivas}, Motivo: {Motivo}",
                command.TraceId, command.FilialId, command.VeiculoId,
                inicio, fim, ocupacaoAtual, filial.CelulasAtivas, "capacidade");

            await _auditLogger.LogAsync(
                evento: "AGENDAMENTO_REJEITADO",
                entidade: "agendamentos",
                entidadeId: null,
                dados: new
                {
                    motivo = "capacidade",
                    filialId = command.FilialId,
                    veiculoId = command.VeiculoId,
                    clienteId = command.ClienteId,
                    inicio,
                    fim,
                    ocupacaoAtual,
                    celulasAtivas = filial.CelulasAtivas,
                },
                cancellationToken: cancellationToken).ConfigureAwait(false);

            throw new CapacidadeFilialAtingidaException();
        }

        var agendamentoId = Guid.NewGuid();
    var agendamento = Agendamento.Criar(
        id: agendamentoId,
        filialId: command.FilialId,
        clienteId: command.ClienteId,
        veiculoId: command.VeiculoId,
        criadoPor: command.UsuarioId ?? Guid.Empty,
        inicio: inicio,
        fim: fim,
        duracaoTotalMin: duracaoTotalMin,
        valorTotal: valorTotal,
        responsavelId: command.ResponsavelId,
        observacoes: command.Observacoes);

        var itens = servicos.Select(s => AgendamentoItem.Criar(
            id: Guid.NewGuid(),
            agendamentoId: agendamentoId,
            servicoId: s.Id,
            precoAplicado: s.Preco,
            duracaoAplicada: s.DuracaoMin)).ToList();

        var historico = AgendamentoHistorico.Registrar(
            id: Guid.NewGuid(),
            agendamentoId: agendamentoId,
            evento: EventoHistorico.Criado,
            usuarioId: command.UsuarioId ?? Guid.Empty);

        await _repositorio.CriarAsync(
            agendamento, itens, historico,
            command.TraceId, command.UsuarioId, cancellationToken);

        _logger.LogInformation(
            "Agendamento criado com sucesso. TraceId: {TraceId}, FilialId: {FilialId}, "
            + "VeiculoId: {VeiculoId}, Inicio: {Inicio}, Fim: {Fim}, "
            + "OcupacaoAtual: {OcupacaoAtual}, CelulasAtivas: {CelulasAtivas}",
            command.TraceId, command.FilialId, command.VeiculoId,
            inicio, fim, ocupacaoAtual, filial.CelulasAtivas);

        return new CriarAgendamentoResponse
        {
            Message = "Agendamento criado com sucesso.",
        Data = new CriarAgendamentoData
        {
            Id = agendamento.Id,
            FilialId = agendamento.FilialId,
            ClienteId = agendamento.ClienteId,
            VeiculoId = agendamento.VeiculoId,
            ResponsavelId = agendamento.ResponsavelId,
            Status = "AGENDADO",
                Inicio = agendamento.Inicio,
                Fim = agendamento.Fim,
                DuracaoTotalMin = agendamento.DuracaoTotalMin,
                ValorTotal = agendamento.ValorTotal,
            },
            TraceId = command.TraceId,
        };
    }
}
