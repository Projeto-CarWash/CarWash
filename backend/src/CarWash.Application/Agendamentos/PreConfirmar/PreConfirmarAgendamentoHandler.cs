using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Agendamentos.Abstractions;
using CarWash.Application.Agendamentos.Common;
using CarWash.Application.Agendamentos.Persistence;
using CarWash.Application.Common.Exceptions;
using Microsoft.Extensions.Logging;

namespace CarWash.Application.Agendamentos.PreConfirmar;

/// <summary>
/// Use case da pré-confirmação do agendamento (RF015 — etapa 1). Valida as
/// dependências, calcula o resumo e o <c>hashResumo</c>, faz o pré-check de
/// conflito de veículo (RN011 — já na prévia, devolve 409) e emite o
/// <c>tokenConfirmacao</c> assinado. NADA é persistido nesta etapa.
/// </summary>
public sealed class PreConfirmarAgendamentoHandler
    : ICommandHandler<PreConfirmarAgendamentoCommand, PreConfirmacaoResponse>
{
    private readonly CalculadoraResumoAgendamento _calculadora;
    private readonly IAgendamentoRepository _agendamentos;
    private readonly ITokenConfirmacaoService _tokens;
    private readonly ILogger<PreConfirmarAgendamentoHandler> _logger;

    public PreConfirmarAgendamentoHandler(
        CalculadoraResumoAgendamento calculadora,
        IAgendamentoRepository agendamentos,
        ITokenConfirmacaoService tokens,
        ILogger<PreConfirmarAgendamentoHandler> logger)
    {
        _calculadora = calculadora;
        _agendamentos = agendamentos;
        _tokens = tokens;
        _logger = logger;
    }

    public async Task<PreConfirmacaoResponse> HandleAsync(
        PreConfirmarAgendamentoCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Defesa em profundidade — o validator já garante, mas um chamador interno
        // que bypasse a pipeline recebe 400 estruturado em vez de 500.
        if (!command.Inicio.HasValue || command.ServicoIds is not { Count: > 0 })
        {
            throw new ValidationException(
                "Dados do agendamento inválidos. Verifique os campos e tente novamente.",
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["servicoIds"] = ["Informe início e ao menos um serviço."],
                });
        }

        var usuarioId = command.UsuarioId ?? throw new ValidationException(
            "Não foi possível identificar o usuário autenticado.",
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["usuario"] = ["Usuário autenticado é obrigatório."],
            });

        // Valida existência/estado, monta o resumo e o hashResumo (RF019/RN010/CA007/CA009).
        var calculado = await _calculadora.CalcularAsync(
            command.FilialId,
            command.ClienteId,
            command.VeiculoId,
            command.ResponsavelId,
            command.Inicio.Value,
            command.ServicoIds,
            command.Observacoes,
            cancellationToken).ConfigureAwait(false);

        // L9: o pré-check de RN011 acontece já na prévia — o usuário sabe do
        // conflito antes de revisar/confirmar (409 na pré-confirmação).
        if (await _agendamentos.ExisteConflitoVeiculoAsync(
            command.VeiculoId,
            calculado.Inicio,
            calculado.Fim,
            cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning(
                "Pré-confirmação rejeitada por conflito RN011 — veículo {VeiculoId} na janela [{Inicio:o}, {Fim:o}). TraceId: {TraceId}",
                command.VeiculoId,
                calculado.Inicio,
                calculado.Fim,
                command.TraceId);
            throw new AgendamentoConflitanteException();
        }

        // RF008/RN009: agendamentos simultâneos são permitidos até o limite de
        // células ativas da filial. Pré-check já na prévia (409) — o teto pode ter
        // sido atingido entre a montagem do formulário e a pré-confirmação.
        if (await _agendamentos.CapacidadeAtingidaAsync(
            command.FilialId,
            calculado.Inicio,
            calculado.Fim,
            cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning(
                "Pré-confirmação rejeitada por capacidade da filial atingida (RF008) — filial {FilialId} na janela [{Inicio:o}, {Fim:o}). TraceId: {TraceId}",
                command.FilialId,
                calculado.Inicio,
                calculado.Fim,
                command.TraceId);
            throw new CapacidadeFilialAtingidaException();
        }

        var token = _tokens.Gerar(calculado.HashResumo, usuarioId, command.TraceId);
        var validado = _tokens.Validar(token, usuarioId);

        _logger.LogInformation(
            "Pré-confirmação gerada. UsuarioId: {UsuarioId}. FilialId: {FilialId}. VeiculoId: {VeiculoId}. "
            + "Janela: [{Inicio:o}, {Fim:o}). HashResumo: {HashResumo}. ExpiraEm: {ExpiraEm:o}. TraceId: {TraceId}",
            usuarioId,
            command.FilialId,
            command.VeiculoId,
            calculado.Inicio,
            calculado.Fim,
            calculado.HashResumo,
            validado.ExpiraEm,
            command.TraceId);

        return new PreConfirmacaoResponse
        {
            TokenConfirmacao = token,
            ExpiraEm = validado.ExpiraEm,
            Resumo = calculado.Resumo,
            TraceId = command.TraceId,
        };
    }
}
