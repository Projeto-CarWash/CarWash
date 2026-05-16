using CarWash.IntegrationTests.Collections;
using CarWash.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace CarWash.IntegrationTests.Schema;

/// <summary>
/// Garantias de DoD §2/§3/§4 — todas as tabelas existem, nenhuma PK usa
/// <c>gen_random_uuid()</c> e a extensão <c>btree_gist</c> está habilitada
/// (pgcrypto, não).
/// </summary>
[Collection(nameof(PostgresCollection))]
public class SchemaShapeTests
{
    private readonly PostgresFixture _fixture;

    public SchemaShapeTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private static readonly string[] TabelasEsperadas =
    [
        "agendamento_historico",
        "agendamento_itens",
        "agendamentos",
        "audit_logs",
        "clientes",
        "feature_flags",
        "filiados",
        "filiais",
        "notificacoes",
        "outbox_eventos",
        "servicos",
        "usuario_preferencias",
        "usuario_sessoes",
        "usuarios",
        "veiculos",
    ];

    [Fact]
    public async Task Todas_as_15_tabelas_existem_em_public()
    {
        var encontradas = await ConsultaScalarLista(
            "SELECT table_name FROM information_schema.tables "
            + "WHERE table_schema = 'public' AND table_type = 'BASE TABLE' "
            + "ORDER BY table_name;").ConfigureAwait(false);

        foreach (var t in TabelasEsperadas)
        {
            encontradas.Should().Contain(t, $"a tabela {t} deve existir após a migration");
        }
    }

    [Fact]
    public async Task Nenhuma_PK_usa_gen_random_uuid()
    {
        await using var conn = new NpgsqlConnection(_fixture.ConnectionString);
        await conn.OpenAsync().ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT count(*) FROM information_schema.columns "
            + "WHERE column_name = 'id' AND table_schema = 'public' "
            + "AND column_default IS NOT NULL;";
        var result = (long)(await cmd.ExecuteScalarAsync().ConfigureAwait(false))!;
        result.Should().Be(0, "ADR 0001: UUID é gerado em C#, sem DEFAULT no banco");
    }

    [Fact]
    public async Task Extensao_btree_gist_habilitada_e_pgcrypto_nao()
    {
        var extensoes = await ConsultaScalarLista(
            "SELECT extname FROM pg_extension;").ConfigureAwait(false);

        extensoes.Should().Contain("btree_gist", "necessária para EXCLUDE constraint do RN011");
        extensoes.Should().NotContain("pgcrypto", "UUID é gerado em C# — pgcrypto desnecessária (ADR 0001)");
    }

    [Fact]
    public async Task EXCLUDE_constraint_RN011_existe_com_definicao_correta()
    {
        await using var conn = new NpgsqlConnection(_fixture.ConnectionString);
        await conn.OpenAsync().ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT pg_get_constraintdef(oid) "
            + "FROM pg_constraint WHERE conname = 'ex_ag_veiculo_janela';";
        var def = (string?)await cmd.ExecuteScalarAsync().ConfigureAwait(false);

        def.Should().NotBeNullOrWhiteSpace();
        def.Should().Contain("EXCLUDE USING gist");
        def.Should().Contain("tstzrange(inicio, fim, '[)'::text)");
        def.Should().Contain("status)::text = 'agendado'");
    }

    [Fact]
    public async Task Indices_obrigatorios_da_DoD_existem()
    {
        var indices = await ConsultaScalarLista(
            "SELECT indexname FROM pg_indexes WHERE schemaname = 'public';").ConfigureAwait(false);

        string[] esperados =
        [
            "uk_usuarios_email", "idx_usuarios_ativo",
            "idx_sessoes_usuario_id", "idx_sessoes_expira_em", "idx_sessoes_revogado_em",
            "uk_filiais_nome", "idx_filiais_ativa",
            "idx_clientes_nome", "idx_clientes_email", "uk_clientes_cpf", "uk_clientes_cnpj",
            "idx_filiados_cliente_id", "idx_filiados_cpf", "uk_filiados_cliente_cpf",
            "uk_veiculos_placa", "idx_veiculos_cliente_id",
            "uk_servicos_nome", "idx_servicos_ativo",
            "idx_ag_filial_inicio", "idx_ag_status", "idx_ag_cliente", "idx_ag_veiculo",
            "idx_item_agendamento", "idx_item_servico", "uk_item_agendamento_servico",
            "idx_hist_agendamento", "idx_hist_evento", "idx_hist_ocorrido_em",
            "idx_audit_evento", "idx_audit_entidade", "idx_audit_criado_em", "idx_audit_correlation",
            "uk_pref_usuario_id",
            "uk_outbox_idempotency", "idx_outbox_status_disponivel",
            "uk_notif_dedupe", "idx_notif_status",
            "uk_flag_nome_ambiente_filial",
        ];

        foreach (var idx in esperados)
        {
            indices.Should().Contain(idx, $"o índice {idx} é exigido pela DoD §6");
        }
    }

    private async Task<List<string>> ConsultaScalarLista(string sql)
    {
        await using var conn = new NpgsqlConnection(_fixture.ConnectionString);
        await conn.OpenAsync().ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
        var lista = new List<string>();
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            lista.Add(reader.GetString(0));
        }

        return lista;
    }
}
