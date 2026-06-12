using System.Globalization;
using CarWash.Application.Abstractions.Messaging;
using CarWash.Application.Agenda.Common;
using CarWash.Application.Agenda.Persistence;

namespace CarWash.Application.Agenda.Consultar;

/// <summary>
/// Use case de consulta de agenda (RF009). Reparseia os parâmetros já validados
/// pelo <see cref="ConsultarAgendaQueryValidator"/>, aplica o mapeamento de status
/// (ADR 0004 — L1) e projeta a fonte única <see cref="AgendaProjecao"/> para o
/// formato simples ou detalhado. Curto-circuita para lista vazia quando o filtro
/// é <c>EM_ANDAMENTO</c> — sem ir ao banco.
/// </summary>
public sealed class ConsultarAgendaHandler : IQueryHandler<ConsultarAgendaQuery, ConsultarAgendaResponse>
{
    /// <summary>Título de fallback quando o agendamento não tem serviços (L2).</summary>
    public const string TituloPadrao = "Agendamento";

    /// <summary>Resumo de serviços quando o agendamento não tem serviços (L3).</summary>
    public const string ServicosResumoVazio = "Sem serviços";

    /// <summary>Mensagem de sucesso com itens.</summary>
    public const string MensagemSucesso = "Agenda consultada com sucesso.";

    /// <summary>Mensagem de sucesso sem itens.</summary>
    public const string MensagemListaVazia = "Nenhum evento encontrado para o período selecionado.";

    private readonly IAgendaRepository _repositorio;

    public ConsultarAgendaHandler(IAgendaRepository repositorio)
    {
        _repositorio = repositorio;
    }

    /// <inheritdoc/>
    public async Task<ConsultarAgendaResponse> HandleAsync(
        ConsultarAgendaQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // O validator já garantiu que tudo parseia — aqui o parse não pode falhar.
        bool detalhado = string.Equals(
            query.Formato?.Trim(),
            "detalhado",
            StringComparison.OrdinalIgnoreCase);

        ConsultarAgendaQueryValidator.TentarParsearData(query.Inicio, out var inicioUtc);
        ConsultarAgendaQueryValidator.TentarParsearData(query.Fim, out var fimUtc);

        var filialId = Guid.Parse(query.FilialId!, CultureInfo.InvariantCulture);
        var clienteId = ParsearGuidOpcional(query.ClienteId);
        var responsavelId = ParsearGuidOpcional(query.UsuarioId);
        string? statusDb = StatusAgendaMapper.ParaDb(query.Status);

        var projecoes = await _repositorio.ConsultarAsync(
            filialId,
            inicioUtc,
            fimUtc,
            clienteId,
            responsavelId,
            statusDb,
            cancellationToken).ConfigureAwait(false);

        if (projecoes.Count == 0)
        {
            return RespostaVazia(query.TraceId);
        }

        IReadOnlyList<object> data = detalhado
            ? projecoes.Select(ProjetarDetalhado).ToList()
            : projecoes.Select(ProjetarSimples).ToList();

        return new ConsultarAgendaResponse
        {
            Message = MensagemSucesso,
            Data = data,
            TraceId = query.TraceId,
        };
    }

    /// <summary>
    /// Deriva o título do formato simples (L2): nome do primeiro serviço na
    /// ordem de criação. Fallback <see cref="TituloPadrao"/> sem serviços.
    /// </summary>
    /// <returns></returns>
    public static string DerivarTitulo(IReadOnlyList<AgendaServicoProjecao> servicos)
    {
        ArgumentNullException.ThrowIfNull(servicos);
        return servicos.Count == 0 ? TituloPadrao : servicos[0].Nome;
    }

    /// <summary>
    /// Deriva o resumo de serviços do formato simples (L3): <c>"&lt;nome&gt;"</c>
    /// para 1 serviço, <c>"&lt;nome&gt; + &lt;N-1&gt;"</c> para N&gt;1,
    /// <see cref="ServicosResumoVazio"/> para 0.
    /// </summary>
    /// <returns></returns>
    public static string DerivarServicosResumo(IReadOnlyList<AgendaServicoProjecao> servicos)
    {
        ArgumentNullException.ThrowIfNull(servicos);

        if (servicos.Count == 0)
        {
            return ServicosResumoVazio;
        }

        if (servicos.Count == 1)
        {
            return servicos[0].Nome;
        }

        string restantes = (servicos.Count - 1).ToString(CultureInfo.InvariantCulture);
        return $"{servicos[0].Nome} + {restantes}";
    }

    private static ConsultarAgendaResponse RespostaVazia(string traceId) => new()
    {
        Message = MensagemListaVazia,
        Data = [],
        TraceId = traceId,
    };

    private static Guid? ParsearGuidOpcional(string? valor) =>
        Guid.TryParse(valor, out var id) ? id : null;

    private static AgendaItemSimplesResponse ProjetarSimples(AgendaProjecao projecao) => new()
    {
        AgendamentoId = projecao.AgendamentoId,
        Inicio = projecao.Inicio,
        Fim = projecao.Fim,
        Titulo = DerivarTitulo(projecao.Servicos),
        Status = StatusAgendaMapper.ParaApi(projecao.Status),
        ClienteNome = projecao.ClienteNome,
        VeiculoPlaca = projecao.VeiculoPlaca,
        ServicosResumo = DerivarServicosResumo(projecao.Servicos),
    };

    private static AgendaItemDetalhadoResponse ProjetarDetalhado(AgendaProjecao projecao) => new()
    {
        AgendamentoId = projecao.AgendamentoId,
        Status = StatusAgendaMapper.ParaApi(projecao.Status),
        FilialId = projecao.FilialId,
        Inicio = projecao.Inicio,
        Fim = projecao.Fim,
        DuracaoTotalMin = projecao.DuracaoTotalMin,
        ValorTotal = projecao.ValorTotal,
        Cliente = new AgendaClienteResponse
        {
            Id = projecao.ClienteId,
            Nome = projecao.ClienteNome,
            CpfCnpj = projecao.ClienteCpf ?? projecao.ClienteCnpj,
            Telefone = projecao.ClienteTelefone,
            Celular = projecao.ClienteCelular,
        },
        Veiculo = new AgendaVeiculoResponse
        {
            Id = projecao.VeiculoId,
            Placa = projecao.VeiculoPlaca,
            Modelo = projecao.VeiculoModelo,
            Fabricante = projecao.VeiculoFabricante,
            Cor = projecao.VeiculoCor,
        },
        Servicos = projecao.Servicos
            .Select(s => new AgendaServicoResponse
            {
                Id = s.Id,
                Nome = s.Nome,
                DuracaoMin = s.DuracaoMin,
                Preco = s.Preco,
            })
            .ToList(),
        Observacoes = projecao.Observacoes,
        CriadoEm = projecao.CriadoEm,
        AtualizadoEm = projecao.AtualizadoEm,
    };
}
