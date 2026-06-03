using System.Data;
using CarWash.Application.Clientes.HistoricoAtendimentos.Common;
using CarWash.Application.Interfaces;
using CarWash.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CarWash.Infrastructure.Repositories;

public sealed class HistoricoAtendimentosClienteRepository : IHistoricoAtendimentosClienteRepository
{
    private readonly CarWashDbContext context;

    public HistoricoAtendimentosClienteRepository(CarWashDbContext context)
    {
        this.context = context;
    }

    public Task<bool> ClienteExisteAsync(
        Guid clienteId,
        CancellationToken cancellationToken)
    {
        return context.Clientes
            .AsNoTracking()
            .AnyAsync(x => x.Id == clienteId, cancellationToken);
    }

    public async Task<(IReadOnlyCollection<HistoricoAtendimentoResponse> Itens, int Total)> ConsultarAsync(
        Guid clienteId,
        DateTimeOffset? dataInicio,
        DateTimeOffset? dataFim,
        int? ultimosDias,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        DateTimeOffset? inicioFiltro = dataInicio;
        DateTimeOffset? fimFiltro = dataFim;

        if (ultimosDias.HasValue)
        {
            inicioFiltro = DateTimeOffset.UtcNow.AddDays(-ultimosDias.Value);
            fimFiltro = DateTimeOffset.UtcNow;
        }

        string? statusNormalizado = string.IsNullOrWhiteSpace(status)
            ? null
            : status.Trim().ToLowerInvariant();

        int offset = (page - 1) * pageSize;

        await using var connection = context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        int total = await ObterTotalAsync(
            connection,
            clienteId,
            inicioFiltro,
            fimFiltro,
            statusNormalizado,
            cancellationToken);

        IReadOnlyCollection<HistoricoAtendimentoLinha> linhas = await ObterLinhasAsync(
            connection,
            clienteId,
            inicioFiltro,
            fimFiltro,
            statusNormalizado,
            pageSize,
            offset,
            cancellationToken);

        List<HistoricoAtendimentoResponse> historico = linhas
            .GroupBy(x => new
            {
                x.AgendamentoId,
                x.Inicio,
                x.Fim,
                x.Status,
                x.FilialId,
                x.FilialNome,
                x.Placa,
                x.Modelo,
                x.ResponsavelId,
                x.ResponsavelNome,
                x.DuracaoTotalMin,
                x.ValorTotal,
                x.ObservacoesLogisticas,
            })
            .Select(group => new HistoricoAtendimentoResponse
            {
                AgendamentoId = group.Key.AgendamentoId,
                Data = DateOnly.FromDateTime(group.Key.Inicio.UtcDateTime.Date),
                HoraInicio = group.Key.Inicio,
                HoraFim = group.Key.Fim,
                Status = group.Key.Status.ToUpperInvariant(),
                Filial = new HistoricoFilialResponse
                {
                    Id = group.Key.FilialId,
                    Nome = group.Key.FilialNome,
                },
                Veiculo = new HistoricoVeiculoResponse
                {
                    Placa = group.Key.Placa,
                    Modelo = group.Key.Modelo,
                },
                Servicos = group
                    .Where(x => x.ServicoId.HasValue)
                    .Select(x => new HistoricoServicoResponse
                    {
                        Id = x.ServicoId!.Value,
                        Nome = x.ServicoNome ?? string.Empty,
                        DuracaoMin = x.DuracaoAplicada ?? 0,
                        Preco = x.PrecoAplicado ?? 0m,
                    })
                    .ToList(),
                DuracaoTotalMin = group.Key.DuracaoTotalMin,
                ValorTotal = group.Key.ValorTotal,
                UsuarioResponsavel = new HistoricoUsuarioResponsavelResponse
                {
                    Id = group.Key.ResponsavelId,
                    Nome = group.Key.ResponsavelNome,
                },
                ObservacoesLogisticas = group.Key.ObservacoesLogisticas,
                MotivoCancelamento = null,
                ConcluidoEm = group.Key.Status.Equals("concluido", StringComparison.OrdinalIgnoreCase)
                    ? group.Key.Fim
                    : null,
                CanceladoEm = group.Key.Status.Equals("cancelado", StringComparison.OrdinalIgnoreCase)
                    ? group.Key.Fim
                    : null,
                Origem = "manual",
            })
            .OrderByDescending(x => x.HoraInicio)
            .ToList();

        return (historico, total);
    }

    private static async Task<int> ObterTotalAsync(
        System.Data.Common.DbConnection connection,
        Guid clienteId,
        DateTimeOffset? dataInicio,
        DateTimeOffset? dataFim,
        string? status,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();

        command.CommandText = """
            select count(*)
            from agendamentos a
            where a.cliente_id = @clienteId
              and (@dataInicio::timestamptz is null or a.inicio >= @dataInicio::timestamptz)
                and (@dataFim::timestamptz is null or a.inicio <= @dataFim::timestamptz)
                and (@status::text is null or lower(a.status) = @status::text);
            """;

        AddParameter(command, "@clienteId", clienteId);
        AddParameter(command, "@dataInicio", dataInicio);
        AddParameter(command, "@dataFim", dataFim);
        AddParameter(command, "@status", status);

        object? result = await command.ExecuteScalarAsync(cancellationToken);

        return Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task<IReadOnlyCollection<HistoricoAtendimentoLinha>> ObterLinhasAsync(
        System.Data.Common.DbConnection connection,
        Guid clienteId,
        DateTimeOffset? dataInicio,
        DateTimeOffset? dataFim,
        string? status,
        int pageSize,
        int offset,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();

        command.CommandText = """
            with agendamentos_filtrados as (
                select
                    a.id,
                    a.filial_id,
                    a.veiculo_id,
                    a.responsavel_id,
                    a.status,
                    a.inicio,
                    a.fim,
                    a.criado_em,
                    a.duracao_total_min,
                    a.valor_total
                from agendamentos a
                where a.cliente_id = @clienteId
                    and (@dataInicio::timestamptz is null or a.inicio >= @dataInicio::timestamptz)
                    and (@dataFim::timestamptz is null or a.inicio <= @dataFim::timestamptz)
                    and (@status::text is null or lower(a.status) = @status::text)
                order by a.inicio desc, a.criado_em desc
                limit @pageSize offset @offset
            ),
            observacoes as (
                select
                    agendamento_id,
                    string_agg(texto, E'\n' order by criado_em desc) as observacoes_logisticas
                from agendamento_observacoes
                where ativo = true
                group by agendamento_id
            )
            select
                a.id as agendamento_id,
                a.inicio,
                a.fim,
                a.status,
                a.duracao_total_min,
                a.valor_total,
                f.id as filial_id,
                f.nome as filial_nome,
                v.placa,
                v.modelo,
                r.id as responsavel_id,
                r.nome as responsavel_nome,
                s.id as servico_id,
                s.nome as servico_nome,
                ai.duracao_aplicada,
                ai.preco_aplicado,
                o.observacoes_logisticas
            from agendamentos_filtrados a
            inner join filiais f on f.id = a.filial_id
            inner join veiculos v on v.id = a.veiculo_id
            inner join filiados r on r.id = a.responsavel_id
            left join agendamento_itens ai on ai.agendamento_id = a.id
            left join servicos s on s.id = ai.servico_id
            left join observacoes o on o.agendamento_id = a.id
            order by a.inicio desc, a.criado_em desc, s.nome asc;
            """;

        AddParameter(command, "@clienteId", clienteId);
        AddParameter(command, "@dataInicio", dataInicio);
        AddParameter(command, "@dataFim", dataFim);
        AddParameter(command, "@status", status);
        AddParameter(command, "@pageSize", pageSize);
        AddParameter(command, "@offset", offset);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var linhas = new List<HistoricoAtendimentoLinha>();

        while (await reader.ReadAsync(cancellationToken))
        {
            linhas.Add(new HistoricoAtendimentoLinha
            {
                AgendamentoId = reader.GetGuid(reader.GetOrdinal("agendamento_id")),
                Inicio = await reader.GetFieldValueAsync<DateTimeOffset>(reader.GetOrdinal("inicio"), cancellationToken),
                Fim = await reader.GetFieldValueAsync<DateTimeOffset>(reader.GetOrdinal("fim"), cancellationToken),
                Status = reader.GetString(reader.GetOrdinal("status")),
                DuracaoTotalMin = reader.GetInt32(reader.GetOrdinal("duracao_total_min")),
                ValorTotal = reader.GetDecimal(reader.GetOrdinal("valor_total")),
                FilialId = reader.GetGuid(reader.GetOrdinal("filial_id")),
                FilialNome = reader.GetString(reader.GetOrdinal("filial_nome")),
                Placa = reader.GetString(reader.GetOrdinal("placa")),
                Modelo = reader.GetString(reader.GetOrdinal("modelo")),
                ResponsavelId = reader.GetGuid(reader.GetOrdinal("responsavel_id")),
                ResponsavelNome = reader.GetString(reader.GetOrdinal("responsavel_nome")),
                ServicoId = await reader.IsDBNullAsync(reader.GetOrdinal("servico_id"), cancellationToken)
                    ? null
                    : reader.GetGuid(reader.GetOrdinal("servico_id")),
                ServicoNome = await reader.IsDBNullAsync(reader.GetOrdinal("servico_nome"), cancellationToken)
                    ? null
                    : reader.GetString(reader.GetOrdinal("servico_nome")),
                DuracaoAplicada = await reader.IsDBNullAsync(reader.GetOrdinal("duracao_aplicada"), cancellationToken)
                    ? null
                    : reader.GetInt32(reader.GetOrdinal("duracao_aplicada")),
                PrecoAplicado = await reader.IsDBNullAsync(reader.GetOrdinal("preco_aplicado"), cancellationToken)
                    ? null
                    : reader.GetDecimal(reader.GetOrdinal("preco_aplicado")),
                ObservacoesLogisticas = await reader.IsDBNullAsync(reader.GetOrdinal("observacoes_logisticas"), cancellationToken)
                    ? null
                    : reader.GetString(reader.GetOrdinal("observacoes_logisticas")),
            });
        }

        return linhas;
    }

    private static void AddParameter(
        System.Data.Common.DbCommand command,
        string name,
        object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private sealed class HistoricoAtendimentoLinha
    {
        public Guid AgendamentoId { get; set; }

        public DateTimeOffset Inicio { get; set; }

        public DateTimeOffset Fim { get; set; }

        public string Status { get; set; } = string.Empty;

        public Guid FilialId { get; set; }

        public string FilialNome { get; set; } = string.Empty;

        public string Placa { get; set; } = string.Empty;

        public string Modelo { get; set; } = string.Empty;

        public Guid ResponsavelId { get; set; }

        public string ResponsavelNome { get; set; } = string.Empty;

        public Guid? ServicoId { get; set; }

        public string? ServicoNome { get; set; }

        public int? DuracaoAplicada { get; set; }

        public decimal? PrecoAplicado { get; set; }

        public int DuracaoTotalMin { get; set; }

        public decimal ValorTotal { get; set; }

        public string? ObservacoesLogisticas { get; set; }
    }
}
