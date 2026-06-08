using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Agendamentos.Common;
using CarWash.Application.Agendamentos.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Domain.Entities;
using CarWash.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CarWash.Application.Agendamentos.Criar;

/// <summary>
/// Use case de criação de agendamento (RF007/RF019/RF020/RF024). RN011 é defendida
/// em camadas: (1) validator estrutural, (2) pré-check de conflito aqui, (3) o
/// método de fábrica do domínio, (4) a constraint EXCLUDE <c>ex_ag_veiculo_janela</c>
/// no banco — esta última fecha a race condition e o repositório a traduz em
/// <see cref="AgendamentoConflitanteException"/> (409).
/// </summary>
/// <remarks>
/// Caminho de criação direta — mantido e marcado <c>[Obsolete]</c> a partir do
/// RF015 (ADR 0004): o frontend passa a usar o fluxo de duas etapas
/// (pré-confirmação + confirmação). Não remover no MVP.
/// </remarks>
#pragma warning disable S1133 // [Obsolete] proposital — RF007 mantido por decisão do ADR 0004; remoção é um card pós-MVP.
[Obsolete("RF015/ADR 0004: prefira o fluxo /pre-confirmacao + /confirmar. Mantido para integrações e testes.")]
#pragma warning restore S1133
public sealed class CriarAgendamentoHandler : ICommandHandler<CriarAgendamentoCommand, AgendamentoResponse>
{
    private readonly IAgendamentoRepository _agendamentos;
    private readonly CalculadoraResumoAgendamento _calculadora;
    private readonly ILogger<CriarAgendamentoHandler> _logger;

    public CriarAgendamentoHandler(
        IAgendamentoRepository agendamentos,
        CalculadoraResumoAgendamento calculadora,
        ILogger<CriarAgendamentoHandler> logger)
    {
        _agendamentos = agendamentos;
        _calculadora = calculadora;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<AgendamentoResponse> HandleAsync(CriarAgendamentoCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Defesa em profundidade: o validator já exige NotNull, mas se um chamador
        // interno bypassar a pipeline, falhamos com 400 estruturado em vez de 500.
        if (!command.Inicio.HasValue || command.ServicoIds is not { Count: > 0 })
        {
            throw new ValidationException(
                "Dados do agendamento inválidos. Verifique os campos e tente novamente.",
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["servicoIds"] = ["Informe início e ao menos um serviço."],
                });
        }

        var criadoPor = command.UsuarioId ?? throw new ValidationException(
            "Não foi possível identificar o usuário autenticado.",
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["usuario"] = ["Usuário autenticado é obrigatório."],
            });

        // --- Validação de existência/estado + cálculo de totais (RF019/RN010/CA007/CA009). ---
        var calculado = await _calculadora.CalcularAsync(
            command.FilialId,
            command.ClienteId,
            command.VeiculoId,
            command.ResponsavelId,
            command.Inicio.Value,
            command.ServicoIds,
            command.Observacoes,
            cancellationToken).ConfigureAwait(false);

        // --- RN011 camada 2: pré-check de conflito antes de inserir. ---
        if (await _agendamentos.ExisteConflitoVeiculoAsync(
            command.VeiculoId,
            calculado.Inicio,
            calculado.Fim,
            cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning(
                "Falha de validação RN011 — veículo {VeiculoId} já agendado na janela [{Inicio:o}, {Fim:o}). TraceId: {TraceId}",
                command.VeiculoId,
                calculado.Inicio,
                calculado.Fim,
                command.TraceId);
            throw new AgendamentoConflitanteException();
        }

        // RF008/RN009: agendamentos simultâneos permitidos até o limite de células
        // ativas da filial; ao exceder o teto, rejeita com 409.
        if (await _agendamentos.CapacidadeAtingidaAsync(
            command.FilialId,
            calculado.Inicio,
            calculado.Fim,
            cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning(
                "Criação rejeitada por capacidade da filial atingida (RF008) — filial {FilialId} na janela [{Inicio:o}, {Fim:o}). TraceId: {TraceId}",
                command.FilialId,
                calculado.Inicio,
                calculado.Fim,
                command.TraceId);
            throw new CapacidadeFilialAtingidaException();
        }

        var agendamentoId = Guid.NewGuid();

        // RN011 camada 3: a fábrica do domínio reforça as invariantes (filial,
        // veículo, início < fim) antes de qualquer persistência.
        var agendamento = Agendamento.Criar(
            id: agendamentoId,
            filialId: command.FilialId,
            clienteId: command.ClienteId,
            veiculoId: command.VeiculoId,
            criadoPor: criadoPor,
            inicio: calculado.Inicio,
            fim: calculado.Fim,
            responsavelId: command.ResponsavelId,
            observacoes: calculado.Observacoes,
            duracaoTotalMin: calculado.DuracaoTotalMin,
            valorTotal: calculado.ValorTotal);

        var itens = calculado.Servicos
            .Select(s => AgendamentoItem.Criar(
                id: Guid.NewGuid(),
                agendamentoId: agendamentoId,
                servicoId: s.Id,
                precoAplicado: s.Preco,
                duracaoAplicada: s.DuracaoMin))
            .ToList();

        var historico = AgendamentoHistorico.Registrar(
            id: Guid.NewGuid(),
            agendamentoId: agendamentoId,
            evento: EventoHistorico.Criado,
            usuarioId: criadoPor);

        // RN011 camada 4: a constraint EXCLUDE captura a race condition; o
        // repositório a traduz em AgendamentoConflitanteException (409).
        // RF024/CA009: o repositório revalida o vínculo responsável→cliente sob
        // SELECT FOR UPDATE dentro da transação — fecha a race condition de
        // alteração de vínculo concorrente.
        await _agendamentos.AdicionarAsync(
            agendamento,
            itens,
            historico,
            command.TraceId,
            command.ResponsavelId,
            command.ClienteId,
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Agendamento criado. AgendamentoId: {AgendamentoId}. VeiculoId: {VeiculoId}. FilialId: {FilialId}. "
            + "ClienteId: {ClienteId}. ResponsavelId: {ResponsavelId}. "
            + "Janela: [{Inicio:o}, {Fim:o}). UsuarioId: {UsuarioId}. TraceId: {TraceId}",
            agendamentoId,
            command.VeiculoId,
            command.FilialId,
            command.ClienteId,
            command.ResponsavelId,
            calculado.Inicio,
            calculado.Fim,
            criadoPor,
            command.TraceId);

        return AgendamentoResponseFactory.Montar(agendamento, itens, calculado.Servicos, calculado.Responsavel, command.TraceId);
    }
}
