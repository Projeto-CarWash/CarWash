using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Agendamentos.Common;
using CarWash.Application.Agendamentos.Persistence;
using CarWash.Application.Common;
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
public sealed class CriarAgendamentoHandler : ICommandHandler<CriarAgendamentoCommand, AgendamentoResponse>
{
    private readonly IAgendamentoRepository _agendamentos;
    private readonly IAgendamentoCatalogoRepository _catalogo;
    private readonly ILogger<CriarAgendamentoHandler> _logger;

    public CriarAgendamentoHandler(
        IAgendamentoRepository agendamentos,
        IAgendamentoCatalogoRepository catalogo,
        ILogger<CriarAgendamentoHandler> logger)
    {
        _agendamentos = agendamentos;
        _catalogo = catalogo;
        _logger = logger;
    }

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

        var inicio = DateTime.SpecifyKind(command.Inicio.Value.ToUniversalTime(), DateTimeKind.Utc);
        var observacoes = InputNormalizer.SanitizeTextOrNull(command.Observacoes);

        // --- Validação de existência e estado das dependências (RF019/RN010/CA007/CA009) ---
        await GarantirFilialAsync(command.FilialId, cancellationToken).ConfigureAwait(false);
        var veiculo = await GarantirVeiculoAsync(command.VeiculoId, cancellationToken).ConfigureAwait(false);
        await GarantirClienteAsync(command.ClienteId, cancellationToken).ConfigureAwait(false);
        GarantirVinculoVeiculoCliente(veiculo, command.ClienteId);
        await GarantirResponsavelAsync(command.ResponsavelId, command.ClienteId, cancellationToken).ConfigureAwait(false);

        var servicos = await GarantirServicosAsync(command.ServicoIds, cancellationToken).ConfigureAwait(false);

        var duracaoTotal = servicos.Sum(s => s.DuracaoMin);
        var valorTotal = servicos.Sum(s => s.Preco);
        var fim = inicio.AddMinutes(duracaoTotal);

        // --- RN011 camada 2: pré-check de conflito antes de inserir. ---
        if (await _agendamentos.ExisteConflitoVeiculoAsync(command.VeiculoId, inicio, fim, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning(
                "Falha de validação RN011 — veículo {VeiculoId} já agendado na janela [{Inicio:o}, {Fim:o}). TraceId: {TraceId}",
                command.VeiculoId,
                inicio,
                fim,
                command.TraceId);
            throw new AgendamentoConflitanteException();
        }

        var criadoPor = command.UsuarioId ?? throw new ValidationException(
            "Não foi possível identificar o usuário autenticado.",
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["usuario"] = ["Usuário autenticado é obrigatório."],
            });

        var agendamentoId = Guid.NewGuid();

        // RN011 camada 3: a fábrica do domínio reforça as invariantes (filial,
        // veículo, início < fim) antes de qualquer persistência.
        var agendamento = Agendamento.Criar(
            id: agendamentoId,
            filialId: command.FilialId,
            clienteId: command.ClienteId,
            veiculoId: command.VeiculoId,
            criadoPor: criadoPor,
            inicio: inicio,
            fim: fim,
            responsavelId: command.ResponsavelId,
            observacoes: observacoes,
            duracaoTotalMin: duracaoTotal,
            valorTotal: valorTotal);

        var itens = servicos
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
        await _agendamentos.AdicionarAsync(agendamento, itens, historico, command.TraceId, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Agendamento criado. AgendamentoId: {AgendamentoId}. VeiculoId: {VeiculoId}. FilialId: {FilialId}. "
            + "Janela: [{Inicio:o}, {Fim:o}). UsuarioId: {UsuarioId}. TraceId: {TraceId}",
            agendamentoId,
            command.VeiculoId,
            command.FilialId,
            inicio,
            fim,
            criadoPor,
            command.TraceId);

        return MontarResposta(agendamento, itens, servicos, command.TraceId);
    }

    private static void GarantirVinculoVeiculoCliente(VeiculoSnapshot veiculo, Guid clienteId)
    {
        // RN002: o veículo informado precisa pertencer ao cliente selecionado.
        if (veiculo.ClienteId != clienteId)
        {
            throw new ValidationException(
                "Dados do agendamento inválidos. Verifique os campos e tente novamente.",
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["veiculoId"] = ["O veículo informado não pertence ao cliente selecionado."],
                });
        }
    }

    private async Task GarantirFilialAsync(Guid filialId, CancellationToken cancellationToken)
    {
        if (!await _catalogo.FilialExisteAsync(filialId, cancellationToken).ConfigureAwait(false))
        {
            throw new NotFoundException("Filial informada não foi encontrada.");
        }

        if (!await _catalogo.FilialAtivaAsync(filialId, cancellationToken).ConfigureAwait(false))
        {
            throw new RecursoInativoException("A filial selecionada está inativa e não aceita agendamentos.");
        }
    }

    private async Task<VeiculoSnapshot> GarantirVeiculoAsync(Guid veiculoId, CancellationToken cancellationToken)
    {
        var veiculo = await _catalogo.ObterVeiculoAsync(veiculoId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Veículo informado não foi encontrado.");

        if (!veiculo.Ativo)
        {
            throw new RecursoInativoException("O veículo selecionado está inativo e não pode ser agendado.");
        }

        return veiculo;
    }

    private async Task GarantirClienteAsync(Guid clienteId, CancellationToken cancellationToken)
    {
        if (!await _catalogo.ClienteExisteAsync(clienteId, cancellationToken).ConfigureAwait(false))
        {
            throw new NotFoundException("Cliente informado não foi encontrado.");
        }

        if (!await _catalogo.ClienteAtivoAsync(clienteId, cancellationToken).ConfigureAwait(false))
        {
            throw new RecursoInativoException("O cliente selecionado está inativo e não pode ser agendado.");
        }
    }

    private async Task GarantirResponsavelAsync(Guid? responsavelId, Guid clienteId, CancellationToken cancellationToken)
    {
        if (!responsavelId.HasValue)
        {
            return;
        }

        var responsavel = await _catalogo.ObterResponsavelAsync(responsavelId.Value, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Responsável informado não foi encontrado.");

        if (!responsavel.Ativo)
        {
            throw new RecursoInativoException("O responsável selecionado está inativo.");
        }

        // CA009: responsável só pode agendar em nome do seu próprio titular.
        if (responsavel.ClienteId != clienteId)
        {
            throw new ValidationException(
                "Dados do agendamento inválidos. Verifique os campos e tente novamente.",
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["responsavelId"] = ["O responsável não pertence ao titular do veículo."],
                });
        }
    }

    private async Task<IReadOnlyList<ServicoSnapshot>> GarantirServicosAsync(
        IReadOnlyList<Guid> servicoIds,
        CancellationToken cancellationToken)
    {
        var encontrados = await _catalogo.ObterServicosAsync(servicoIds, cancellationToken).ConfigureAwait(false);

        var ausentes = servicoIds
            .Where(id => encontrados.All(s => s.Id != id))
            .ToList();
        if (ausentes.Count > 0)
        {
            throw new NotFoundException("Um ou mais serviços informados não foram encontrados.");
        }

        var inativos = encontrados.Where(s => !s.Ativo).ToList();
        if (inativos.Count > 0)
        {
            throw new RecursoInativoException(
                "Um ou mais serviços selecionados estão inativos e não podem ser agendados.");
        }

        // Preserva a ordem informada pelo cliente.
        return servicoIds
            .Select(id => encontrados.First(s => s.Id == id))
            .ToList();
    }

    private static AgendamentoResponse MontarResposta(
        Agendamento agendamento,
        IReadOnlyCollection<AgendamentoItem> itens,
        IReadOnlyCollection<ServicoSnapshot> servicos,
        string traceId) => new()
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
            Itens = itens
                .Select(item => new AgendamentoServicoResponse
                {
                    Id = item.Id,
                    ServicoId = item.ServicoId,
                    NomeServico = servicos.First(s => s.Id == item.ServicoId).Nome,
                    PrecoAplicado = item.PrecoAplicado,
                    DuracaoAplicada = item.DuracaoAplicada,
                })
                .ToList(),
            CriadoEm = agendamento.CriadoEm,
            Mensagem = "Agendamento criado com sucesso.",
            TraceId = traceId,
        };
}
