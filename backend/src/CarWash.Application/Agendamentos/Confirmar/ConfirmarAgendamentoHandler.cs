using System.Text.Json;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Agendamentos.Abstractions;
using CarWash.Application.Agendamentos.Common;
using CarWash.Application.Agendamentos.Persistence;
using CarWash.Application.Common.Exceptions;
using CarWash.Domain.Entities;
using CarWash.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CarWash.Application.Agendamentos.Confirmar;

/// <summary>
/// Use case da confirmação do agendamento (RF015 — etapa 2). Persiste o
/// agendamento em transação única e idempotente. A ordem das verificações segue
/// o desenho do arquiteto (L7): (1) validação estrutural — já feita pelo validator
/// no endpoint; (2) token de confirmação; (3) lookup de idempotência;
/// (4) divergência de resumo; (5) pré-check de RN011; (6) persistência (que
/// inclui o registro de idempotência e fecha a race condition no banco).
/// </summary>
public sealed class ConfirmarAgendamentoHandler
    : ICommandHandler<ConfirmarAgendamentoCommand, ConfirmarAgendamentoResultado>
{
    /// <summary>Escopo lógico do registro de idempotência desta operação.</summary>
    public const string EscopoIdempotencia = "agendamento-confirmar";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly CalculadoraResumoAgendamento _calculadora;
    private readonly IAgendamentoRepository _agendamentos;
    private readonly IIdempotenciaRepository _idempotencia;
    private readonly ITokenConfirmacaoService _tokens;
    private readonly ILogger<ConfirmarAgendamentoHandler> _logger;

    public ConfirmarAgendamentoHandler(
        CalculadoraResumoAgendamento calculadora,
        IAgendamentoRepository agendamentos,
        IIdempotenciaRepository idempotencia,
        ITokenConfirmacaoService tokens,
        ILogger<ConfirmarAgendamentoHandler> logger)
    {
        _calculadora = calculadora;
        _agendamentos = agendamentos;
        _idempotencia = idempotencia;
        _tokens = tokens;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ConfirmarAgendamentoResultado> HandleAsync(
        ConfirmarAgendamentoCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Defesa em profundidade — o validator já garante todos estes itens.
        if (!command.Inicio.HasValue
            || command.ServicoIds is not { Count: > 0 }
            || string.IsNullOrWhiteSpace(command.TokenConfirmacao)
            || command.IdempotencyKey is not { } idempotencyKey
            || idempotencyKey == Guid.Empty)
        {
            throw new ValidationException(
                "Dados do agendamento inválidos. Verifique os campos e tente novamente.",
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["body"] = ["Informe início, serviços, token de confirmação e chave de idempotência."],
                });
        }

        var usuarioId = command.UsuarioId ?? throw new ValidationException(
            "Não foi possível identificar o usuário autenticado.",
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["usuario"] = ["Usuário autenticado é obrigatório."],
            });

        // Etapa 2 do L7 — valida o token de confirmação: formato, assinatura ou
        // usuário inválidos resultam em 400; token íntegro porém expirado, em 410.
        var token = _tokens.Validar(command.TokenConfirmacao, usuarioId);

        // Etapa 3 do L7 — lookup de idempotência: replay com mesmo payload devolve
        // a resposta gravada; mesma chave com payload diferente vira conflito.
        var registroExistente = await _idempotencia
            .ObterAsync(idempotencyKey, EscopoIdempotencia, cancellationToken)
            .ConfigureAwait(false);

        // Etapa 4 do L7 — recalcula o resumo e compara o hash com o do token.
        var calculado = await _calculadora.CalcularAsync(
            command.FilialId,
            command.ClienteId,
            command.VeiculoId,
            command.ResponsavelId,
            command.Inicio.Value,
            command.ServicoIds,
            command.Observacoes,
            cancellationToken).ConfigureAwait(false);

        if (!string.Equals(calculado.HashResumo, token.HashResumo, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "Confirmação rejeitada por divergência de resumo. UsuarioId: {UsuarioId}. "
                + "HashEsperado: {HashToken}. HashAtual: {HashAtual}. TraceId: {TraceId}",
                usuarioId,
                token.HashResumo,
                calculado.HashResumo,
                command.TraceId);
            throw new ResumoDivergenteException();
        }

        if (registroExistente is not null)
        {
            return ResolverIdempotencia(registroExistente, calculado.HashResumo, idempotencyKey, command.TraceId);
        }

        // Etapa 5 do L7 — pré-check de RN011: o horário pode ter sido tomado
        // entre a prévia e a confirmação. Mensagem específica do RF015.
        if (await _agendamentos.ExisteConflitoVeiculoAsync(
            command.VeiculoId,
            calculado.Inicio,
            calculado.Fim,
            cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning(
                "Confirmação rejeitada por conflito RN011 — veículo {VeiculoId} na janela [{Inicio:o}, {Fim:o}). TraceId: {TraceId}",
                command.VeiculoId,
                calculado.Inicio,
                calculado.Fim,
                command.TraceId);
            throw new AgendamentoConflitanteException(AgendamentoConflitanteException.MensagemConfirmacao);
        }

        // RF008/RN009: revalida o teto de células ativas da filial — entre a prévia
        // e a confirmação outras requisições podem ter ocupado as células livres.
        // Atendimentos simultâneos são permitidos até o limite (409 ao exceder).
        if (await _agendamentos.CapacidadeAtingidaAsync(
            command.FilialId,
            calculado.Inicio,
            calculado.Fim,
            cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning(
                "Confirmação rejeitada por capacidade da filial atingida (RF008) — filial {FilialId} na janela [{Inicio:o}, {Fim:o}). TraceId: {TraceId}",
                command.FilialId,
                calculado.Inicio,
                calculado.Fim,
                command.TraceId);
            throw new CapacidadeFilialAtingidaException();
        }

        // Etapa 6 do L7 — persistência: agendamento + itens + histórico +
        // idempotência na mesma transação. A UNIQUE da idempotência e a EXCLUDE
        // da RN011 fecham a race condition de requisições concorrentes.
        return await PersistirAsync(command, usuarioId, idempotencyKey, calculado, cancellationToken)
            .ConfigureAwait(false);
    }

    private ConfirmarAgendamentoResultado ResolverIdempotencia(
        IdempotenciaRequisicao registro,
        string hashAtual,
        Guid idempotencyKey,
        string traceId)
    {
        // Mesma chave + mesmo payload → replay legítimo: devolve a resposta gravada.
        if (string.Equals(registro.PayloadHash, hashAtual, StringComparison.Ordinal))
        {
            _logger.LogInformation(
                "Replay idempotente da confirmação. IdempotencyKey: {IdempotencyKey}. "
                + "AgendamentoId: {AgendamentoId}. TraceId: {TraceId}",
                idempotencyKey,
                registro.RecursoId,
                traceId);
            return ConfirmarAgendamentoResultado.Replay(Desserializar(registro.RespostaJson));
        }

        // Mesma chave + payload diferente → uso indevido da chave.
        _logger.LogWarning(
            "Conflito de idempotência — chave {IdempotencyKey} reutilizada com payload distinto. TraceId: {TraceId}",
            idempotencyKey,
            traceId);
        throw new IdempotenciaConflitanteException();
    }

    private async Task<ConfirmarAgendamentoResultado> PersistirAsync(
        ConfirmarAgendamentoCommand command,
        Guid usuarioId,
        Guid idempotencyKey,
        ResumoAgendamentoCalculado calculado,
        CancellationToken cancellationToken)
    {
        var agendamentoId = Guid.NewGuid();

        var agendamento = Agendamento.Criar(
            id: agendamentoId,
            filialId: command.FilialId,
            clienteId: command.ClienteId,
            veiculoId: command.VeiculoId,
            criadoPor: usuarioId,
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
            usuarioId: usuarioId);

        var resposta = AgendamentoResponseFactory.Montar(agendamento, itens, calculado.Servicos, command.TraceId);

        var idempotencia = IdempotenciaRequisicao.Registrar(
            id: Guid.NewGuid(),
            idempotencyKey: idempotencyKey,
            escopo: EscopoIdempotencia,
            usuarioId: usuarioId,
            payloadHash: calculado.HashResumo,
            statusHttp: 201,
            respostaJson: JsonSerializer.Serialize(resposta, JsonOptions),
            recursoId: agendamentoId);

        var resultado = await _agendamentos.AdicionarComIdempotenciaAsync(
            agendamento,
            itens,
            historico,
            idempotencia,
            command.TraceId,
            cancellationToken).ConfigureAwait(false);

        // Corrida vencida por outra requisição com a MESMA chave: a UNIQUE da
        // idempotência disparou e o repositório releu o registro vencedor.
        if (resultado.EhReplay)
        {
            _logger.LogInformation(
                "Replay idempotente resolvido na persistência (corrida concorrente). "
                + "IdempotencyKey: {IdempotencyKey}. TraceId: {TraceId}",
                idempotencyKey,
                command.TraceId);
            return ConfirmarAgendamentoResultado.Replay(
                Desserializar(resultado.RespostaJsonOriginal!));
        }

        _logger.LogInformation(
            "Agendamento confirmado. AgendamentoId: {AgendamentoId}. UsuarioId: {UsuarioId}. "
            + "FilialId: {FilialId}. VeiculoId: {VeiculoId}. Janela: [{Inicio:o}, {Fim:o}). "
            + "ValorTotal: {ValorTotal}. IdempotencyKey: {IdempotencyKey}. TraceId: {TraceId}",
            agendamentoId,
            usuarioId,
            command.FilialId,
            command.VeiculoId,
            calculado.Inicio,
            calculado.Fim,
            calculado.ValorTotal,
            idempotencyKey,
            command.TraceId);

        return ConfirmarAgendamentoResultado.Novo(resposta);
    }

    private static AgendamentoResponse Desserializar(string respostaJson) =>
        JsonSerializer.Deserialize<AgendamentoResponse>(respostaJson, JsonOptions)
        ?? throw new InvalidOperationException(
            "Resposta de idempotência gravada não pôde ser desserializada.");
}
