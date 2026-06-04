using System.Data;
using CarWash.Application.Dashboard.Metricas.Common;
using CarWash.Application.Interfaces;
using CarWash.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CarWash.Infrastructure.Repositories;

public sealed class DashboardMetricasRepository : IDashboardMetricasRepository
{
    private readonly CarWashDbContext context;

    public DashboardMetricasRepository(CarWashDbContext context)
    {
        this.context = context;
    }

    public async Task<DashboardMetricasDataResponse> ConsultarAsync(
        DateTimeOffset dataInicio,
        DateTimeOffset dataFim,
        Guid? filialId,
        Guid? clienteId,
        string? status,
        CancellationToken cancellationToken)
    {
        string? statusNormalizado = NormalizarStatus(status);

        await using var connection = context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        DashboardOperacionalResponse operacional = await ConsultarOperacionalAsync(
            connection,
            dataInicio,
            dataFim,
            filialId,
            clienteId,
            statusNormalizado,
            cancellationToken);

        DashboardFinanceiroResponse financeiro = await ConsultarFinanceiroAsync(
            connection,
            dataInicio,
            dataFim,
            filialId,
            clienteId,
            statusNormalizado,
            cancellationToken);

        financeiro.FaturamentoPorFilial = await ConsultarFaturamentoPorFilialAsync(
            connection,
            dataInicio,
            dataFim,
            filialId,
            clienteId,
            statusNormalizado,
            cancellationToken);

        financeiro.FaturamentoPorServico = await ConsultarFaturamentoPorServicoAsync(
            connection,
            dataInicio,
            dataFim,
            filialId,
            clienteId,
            statusNormalizado,
            cancellationToken);

        return new DashboardMetricasDataResponse
        {
            Periodo = new DashboardPeriodoResponse
            {
                DataInicio = dataInicio,
                DataFim = dataFim,
            },
            FiltrosAplicados = new DashboardFiltrosAplicadosResponse
            {
                FilialId = filialId,
                ClienteId = clienteId,
                Status = status,
            },
            Operacional = operacional,
            Financeiro = financeiro,
        };
    }

    private static async Task<DashboardOperacionalResponse> ConsultarOperacionalAsync(
        System.Data.Common.DbConnection connection,
        DateTimeOffset dataInicio,
        DateTimeOffset dataFim,
        Guid? filialId,
        Guid? clienteId,
        string? status,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();

        command.CommandText = """
            select
                count(*)::int as total_atendimentos,
                count(*) filter (where lower(status) = 'agendado')::int as pendentes,
                count(*) filter (where lower(status) = 'finalizado')::int as concluidos,
                count(*) filter (where lower(status) = 'cancelado')::int as cancelados,
                coalesce(avg(duracao_total_min), 0)::int as tempo_medio
            from agendamentos
            where inicio >= @dataInicio::timestamptz
              and inicio <= @dataFim::timestamptz
              and (@filialId::uuid is null or filial_id = @filialId::uuid)
              and (@clienteId::uuid is null or cliente_id = @clienteId::uuid)
              and (@status::text is null or lower(status) = @status::text);
            """;

        AddParameter(command, "@dataInicio", dataInicio);
        AddParameter(command, "@dataFim", dataFim);
        AddParameter(command, "@filialId", filialId);
        AddParameter(command, "@clienteId", clienteId);
        AddParameter(command, "@status", status);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return new DashboardOperacionalResponse();
        }

        int total = reader.GetInt32(reader.GetOrdinal("total_atendimentos"));
        int concluidos = reader.GetInt32(reader.GetOrdinal("concluidos"));

        decimal taxaConclusao = total == 0
            ? 0
            : Math.Round((decimal)concluidos / total, 4);

        return new DashboardOperacionalResponse
        {
            TotalAtendimentos = total,
            Pendentes = reader.GetInt32(reader.GetOrdinal("pendentes")),
            Concluidos = concluidos,
            Cancelados = reader.GetInt32(reader.GetOrdinal("cancelados")),
            TaxaConclusao = taxaConclusao,
            TempoMedioAtendimentoMin = reader.GetInt32(reader.GetOrdinal("tempo_medio")),
        };
    }

    private static async Task<DashboardFinanceiroResponse> ConsultarFinanceiroAsync(
        System.Data.Common.DbConnection connection,
        DateTimeOffset dataInicio,
        DateTimeOffset dataFim,
        Guid? filialId,
        Guid? clienteId,
        string? status,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();

        command.CommandText = """
            select
                coalesce(sum(valor_total) filter (where lower(status) = 'finalizado'), 0)::numeric(10,2) as faturamento_total,
                count(*) filter (where lower(status) = 'finalizado')::int as atendimentos_concluidos,
                count(distinct cliente_id) filter (where lower(status) = 'finalizado')::int as clientes_com_faturamento
            from agendamentos
            where inicio >= @dataInicio::timestamptz
              and inicio <= @dataFim::timestamptz
              and (@filialId::uuid is null or filial_id = @filialId::uuid)
              and (@clienteId::uuid is null or cliente_id = @clienteId::uuid)
              and (@status::text is null or lower(status) = @status::text);
            """;

        AddParameter(command, "@dataInicio", dataInicio);
        AddParameter(command, "@dataFim", dataFim);
        AddParameter(command, "@filialId", filialId);
        AddParameter(command, "@clienteId", clienteId);
        AddParameter(command, "@status", status);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return new DashboardFinanceiroResponse();
        }

        decimal faturamentoTotal = reader.GetDecimal(reader.GetOrdinal("faturamento_total"));
        int atendimentosConcluidos = reader.GetInt32(reader.GetOrdinal("atendimentos_concluidos"));
        int clientesComFaturamento = reader.GetInt32(reader.GetOrdinal("clientes_com_faturamento"));

        return new DashboardFinanceiroResponse
        {
            FaturamentoTotal = faturamentoTotal,
            TicketMedio = atendimentosConcluidos == 0
                ? 0
                : Math.Round(faturamentoTotal / atendimentosConcluidos, 2),
            ValorMedioPorCliente = clientesComFaturamento == 0
                ? 0
                : Math.Round(faturamentoTotal / clientesComFaturamento, 2),
        };
    }

    private static async Task<List<DashboardFaturamentoFilialResponse>> ConsultarFaturamentoPorFilialAsync(
        System.Data.Common.DbConnection connection,
        DateTimeOffset dataInicio,
        DateTimeOffset dataFim,
        Guid? filialId,
        Guid? clienteId,
        string? status,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();

        command.CommandText = """
            select
                f.id as filial_id,
                f.nome,
                coalesce(sum(a.valor_total), 0)::numeric(10,2) as valor
            from agendamentos a
            inner join filiais f on f.id = a.filial_id
            where a.inicio >= @dataInicio::timestamptz
              and a.inicio <= @dataFim::timestamptz
              and lower(a.status) = 'finalizado'
              and (@filialId::uuid is null or a.filial_id = @filialId::uuid)
              and (@clienteId::uuid is null or a.cliente_id = @clienteId::uuid)
              and (@status::text is null or lower(a.status) = @status::text)
            group by f.id, f.nome
            order by valor desc, f.nome asc;
            """;

        AddParameter(command, "@dataInicio", dataInicio);
        AddParameter(command, "@dataFim", dataFim);
        AddParameter(command, "@filialId", filialId);
        AddParameter(command, "@clienteId", clienteId);
        AddParameter(command, "@status", status);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var resultado = new List<DashboardFaturamentoFilialResponse>();

        while (await reader.ReadAsync(cancellationToken))
        {
            resultado.Add(new DashboardFaturamentoFilialResponse
            {
                FilialId = reader.GetGuid(reader.GetOrdinal("filial_id")),
                Nome = reader.GetString(reader.GetOrdinal("nome")),
                Valor = reader.GetDecimal(reader.GetOrdinal("valor")),
            });
        }

        return resultado;
    }

    private static async Task<List<DashboardFaturamentoServicoResponse>> ConsultarFaturamentoPorServicoAsync(
        System.Data.Common.DbConnection connection,
        DateTimeOffset dataInicio,
        DateTimeOffset dataFim,
        Guid? filialId,
        Guid? clienteId,
        string? status,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();

        command.CommandText = """
            select
                s.id as servico_id,
                s.nome,
                coalesce(sum(ai.preco_aplicado), 0)::numeric(10,2) as valor
            from agendamentos a
            inner join agendamento_itens ai on ai.agendamento_id = a.id
            inner join servicos s on s.id = ai.servico_id
            where a.inicio >= @dataInicio::timestamptz
              and a.inicio <= @dataFim::timestamptz
              and lower(a.status) = 'finalizado'
              and (@filialId::uuid is null or a.filial_id = @filialId::uuid)
              and (@clienteId::uuid is null or a.cliente_id = @clienteId::uuid)
              and (@status::text is null or lower(a.status) = @status::text)
            group by s.id, s.nome
            order by valor desc, s.nome asc;
            """;

        AddParameter(command, "@dataInicio", dataInicio);
        AddParameter(command, "@dataFim", dataFim);
        AddParameter(command, "@filialId", filialId);
        AddParameter(command, "@clienteId", clienteId);
        AddParameter(command, "@status", status);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var resultado = new List<DashboardFaturamentoServicoResponse>();

        while (await reader.ReadAsync(cancellationToken))
        {
            resultado.Add(new DashboardFaturamentoServicoResponse
            {
                ServicoId = reader.GetGuid(reader.GetOrdinal("servico_id")),
                Nome = reader.GetString(reader.GetOrdinal("nome")),
                Valor = reader.GetDecimal(reader.GetOrdinal("valor")),
            });
        }

        return resultado;
    }

    private static string? NormalizarStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        return status.Trim().ToUpperInvariant() switch
        {
            "AGENDADO" => "agendado",
            "CANCELADO" => "cancelado",
            "CONCLUIDO" => "finalizado",
            "FINALIZADO" => "finalizado",
            _ => status.Trim().ToLowerInvariant(),
        };
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
}
