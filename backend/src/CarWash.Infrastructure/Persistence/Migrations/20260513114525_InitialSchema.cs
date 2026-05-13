using System;
using CarWash.Infrastructure.Security;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarWash.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            // RN011 — EXCLUDE constraint depende de btree_gist. Única extensão habilitada
            // no MVP. pgcrypto NÃO é criada (UUID gerado em C# — ADR 0001).
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");

            migrationBuilder.CreateTable(
                name: "clientes",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    cpf = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    cnpj = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    telefone = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    celular = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    endereco = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    observacoes = table.Column<string>(type: "text", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criado_em = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    atualizado_em = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clientes", x => x.id);
                    table.CheckConstraint("ck_clientes_cpf_ou_cnpj", "cpf IS NOT NULL OR cnpj IS NOT NULL");
                });

            migrationBuilder.CreateTable(
                name: "filiais",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ativa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    celulas_ativas = table.Column<int>(type: "integer", nullable: false),
                    timezone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "America/Sao_Paulo"),
                    criado_em = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    atualizado_em = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filiais", x => x.id);
                    table.CheckConstraint("ck_filiais_celulas_faixa", "celulas_ativas BETWEEN 1 AND 100");
                });

            migrationBuilder.CreateTable(
                name: "outbox_eventos",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    evento = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    agregado = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    agregado_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pendente"),
                    tentativas = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    disponivel_em = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    criado_em = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    processado_em = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_eventos", x => x.id);
                    table.CheckConstraint("ck_outbox_status", "status IN ('pendente','processando','processado','falha')");
                    table.CheckConstraint("ck_outbox_tentativas", "tentativas >= 0");
                });

            migrationBuilder.CreateTable(
                name: "servicos",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    preco = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    duracao_min = table.Column<int>(type: "integer", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criado_em = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    atualizado_em = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_servicos", x => x.id);
                    table.CheckConstraint("ck_servicos_duracao", "duracao_min > 0");
                    table.CheckConstraint("ck_servicos_preco", "preco > 0");
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    senha_hash = table.Column<string>(type: "text", nullable: false),
                    perfil = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criado_em = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    atualizado_em = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuarios", x => x.id);
                    table.CheckConstraint("ck_usuarios_perfil", "perfil IN ('ADMIN','FUNCIONARIO')");
                });

            migrationBuilder.CreateTable(
                name: "filiados",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    telefone = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    cpf = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    rg = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criado_em = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    atualizado_em = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filiados", x => x.id);
                    table.CheckConstraint("ck_filiados_cpf_ou_rg", "cpf IS NOT NULL OR rg IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_filiados_cliente",
                        column: x => x.cliente_id,
                        principalSchema: "public",
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "veiculos",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    placa = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    modelo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    fabricante = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    cor = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ano = table.Column<int>(type: "integer", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criado_em = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    atualizado_em = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_veiculos", x => x.id);
                    table.CheckConstraint("ck_veiculos_ano", "ano IS NULL OR (ano BETWEEN 1900 AND 2100)");
                    table.ForeignKey(
                        name: "fk_veiculos_cliente",
                        column: x => x.cliente_id,
                        principalSchema: "public",
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    evento = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    entidade = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    entidade_id = table.Column<Guid>(type: "uuid", nullable: true),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    dados = table.Column<string>(type: "jsonb", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_usuario",
                        column: x => x.usuario_id,
                        principalSchema: "public",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "feature_flags",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ambiente = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    filial_id = table.Column<Guid>(type: "uuid", nullable: true),
                    habilitada = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    valor_json = table.Column<string>(type: "jsonb", nullable: true),
                    atualizado_por = table.Column<Guid>(type: "uuid", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_feature_flags", x => x.id);
                    table.ForeignKey(
                        name: "fk_flag_atualizado_por",
                        column: x => x.atualizado_por,
                        principalSchema: "public",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_flag_filial",
                        column: x => x.filial_id,
                        principalSchema: "public",
                        principalTable: "filiais",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usuario_preferencias",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tema = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "claro"),
                    atualizado_em = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuario_preferencias", x => x.id);
                    table.CheckConstraint("ck_pref_tema", "tema IN ('claro','escuro')");
                    table.ForeignKey(
                        name: "fk_pref_usuario",
                        column: x => x.usuario_id,
                        principalSchema: "public",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usuario_sessoes",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    refresh_token_hash = table.Column<string>(type: "text", nullable: false),
                    expira_em = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    revogado_em = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    ip_origem = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuario_sessoes", x => x.id);
                    table.ForeignKey(
                        name: "fk_sessoes_usuario",
                        column: x => x.usuario_id,
                        principalSchema: "public",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "agendamentos",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    filial_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    veiculo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    responsavel_id = table.Column<Guid>(type: "uuid", nullable: true),
                    criado_por = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "agendado"),
                    inicio = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    fim = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    observacoes = table.Column<string>(type: "text", nullable: true),
                    versao = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    criado_em = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    atualizado_em = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agendamentos", x => x.id);
                    table.CheckConstraint("ck_ag_inicio_menor_fim", "inicio < fim");
                    table.CheckConstraint("ck_ag_status", "status IN ('agendado','cancelado','finalizado')");
                    table.ForeignKey(
                        name: "fk_ag_cliente",
                        column: x => x.cliente_id,
                        principalSchema: "public",
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ag_criado_por",
                        column: x => x.criado_por,
                        principalSchema: "public",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ag_filial",
                        column: x => x.filial_id,
                        principalSchema: "public",
                        principalTable: "filiais",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ag_responsavel",
                        column: x => x.responsavel_id,
                        principalSchema: "public",
                        principalTable: "filiados",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ag_veiculo",
                        column: x => x.veiculo_id,
                        principalSchema: "public",
                        principalTable: "veiculos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "agendamento_historico",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agendamento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evento = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: true),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ocorrido_em = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agendamento_historico", x => x.id);
                    table.CheckConstraint("ck_hist_evento", "evento IN ('CRIADO','EDITADO','CANCELADO','FINALIZADO')");
                    table.ForeignKey(
                        name: "fk_hist_agendamento",
                        column: x => x.agendamento_id,
                        principalSchema: "public",
                        principalTable: "agendamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_hist_usuario",
                        column: x => x.usuario_id,
                        principalSchema: "public",
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "agendamento_itens",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agendamento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    servico_id = table.Column<Guid>(type: "uuid", nullable: false),
                    preco_aplicado = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    duracao_aplicada = table.Column<int>(type: "integer", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agendamento_itens", x => x.id);
                    table.CheckConstraint("ck_item_duracao", "duracao_aplicada > 0");
                    table.CheckConstraint("ck_item_preco", "preco_aplicado >= 0");
                    table.ForeignKey(
                        name: "fk_item_agendamento",
                        column: x => x.agendamento_id,
                        principalSchema: "public",
                        principalTable: "agendamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_item_servico",
                        column: x => x.servico_id,
                        principalSchema: "public",
                        principalTable: "servicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notificacoes",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agendamento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    canal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    destino = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pendente"),
                    tentativas = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ultima_tentativa = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notificacoes", x => x.id);
                    table.CheckConstraint("ck_notif_canal", "canal IN ('email','whatsapp','sms')");
                    table.CheckConstraint("ck_notif_tentativas", "tentativas >= 0");
                    table.ForeignKey(
                        name: "fk_notif_agendamento",
                        column: x => x.agendamento_id,
                        principalSchema: "public",
                        principalTable: "agendamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_hist_agendamento",
                schema: "public",
                table: "agendamento_historico",
                columns: new[] { "agendamento_id", "ocorrido_em" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_hist_evento",
                schema: "public",
                table: "agendamento_historico",
                column: "evento");

            migrationBuilder.CreateIndex(
                name: "idx_hist_ocorrido_em",
                schema: "public",
                table: "agendamento_historico",
                column: "ocorrido_em");

            migrationBuilder.CreateIndex(
                name: "ix_agendamento_historico_usuario_id",
                schema: "public",
                table: "agendamento_historico",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "idx_item_agendamento",
                schema: "public",
                table: "agendamento_itens",
                column: "agendamento_id");

            migrationBuilder.CreateIndex(
                name: "idx_item_servico",
                schema: "public",
                table: "agendamento_itens",
                column: "servico_id");

            migrationBuilder.CreateIndex(
                name: "uk_item_agendamento_servico",
                schema: "public",
                table: "agendamento_itens",
                columns: new[] { "agendamento_id", "servico_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_ag_cliente",
                schema: "public",
                table: "agendamentos",
                columns: new[] { "cliente_id", "inicio" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_ag_filial_inicio",
                schema: "public",
                table: "agendamentos",
                columns: new[] { "filial_id", "inicio" });

            migrationBuilder.CreateIndex(
                name: "idx_ag_status",
                schema: "public",
                table: "agendamentos",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_ag_veiculo",
                schema: "public",
                table: "agendamentos",
                columns: new[] { "veiculo_id", "inicio" });

            migrationBuilder.CreateIndex(
                name: "ix_agendamentos_criado_por",
                schema: "public",
                table: "agendamentos",
                column: "criado_por");

            migrationBuilder.CreateIndex(
                name: "ix_agendamentos_responsavel_id",
                schema: "public",
                table: "agendamentos",
                column: "responsavel_id");

            migrationBuilder.CreateIndex(
                name: "idx_audit_correlation",
                schema: "public",
                table: "audit_logs",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "idx_audit_criado_em",
                schema: "public",
                table: "audit_logs",
                column: "criado_em",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_audit_entidade",
                schema: "public",
                table: "audit_logs",
                columns: new[] { "entidade", "entidade_id" });

            migrationBuilder.CreateIndex(
                name: "idx_audit_evento",
                schema: "public",
                table: "audit_logs",
                columns: new[] { "evento", "criado_em" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_usuario_id",
                schema: "public",
                table: "audit_logs",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "idx_clientes_email",
                schema: "public",
                table: "clientes",
                column: "email",
                filter: "email IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_clientes_nome",
                schema: "public",
                table: "clientes",
                column: "nome");

            migrationBuilder.CreateIndex(
                name: "uk_clientes_cnpj",
                schema: "public",
                table: "clientes",
                column: "cnpj",
                unique: true,
                filter: "cnpj IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uk_clientes_cpf",
                schema: "public",
                table: "clientes",
                column: "cpf",
                unique: true,
                filter: "cpf IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_feature_flags_atualizado_por",
                schema: "public",
                table: "feature_flags",
                column: "atualizado_por");

            migrationBuilder.CreateIndex(
                name: "ix_feature_flags_filial_id",
                schema: "public",
                table: "feature_flags",
                column: "filial_id");

            migrationBuilder.CreateIndex(
                name: "idx_filiados_cliente_id",
                schema: "public",
                table: "filiados",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "idx_filiados_cpf",
                schema: "public",
                table: "filiados",
                column: "cpf",
                filter: "cpf IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uk_filiados_cliente_cpf",
                schema: "public",
                table: "filiados",
                columns: new[] { "cliente_id", "cpf" },
                unique: true,
                filter: "cpf IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_filiais_ativa",
                schema: "public",
                table: "filiais",
                column: "ativa",
                filter: "ativa = true");

            migrationBuilder.CreateIndex(
                name: "uk_filiais_nome",
                schema: "public",
                table: "filiais",
                column: "nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_notif_status",
                schema: "public",
                table: "notificacoes",
                columns: new[] { "status", "criado_em" });

            migrationBuilder.CreateIndex(
                name: "uk_notif_dedupe",
                schema: "public",
                table: "notificacoes",
                columns: new[] { "agendamento_id", "tipo", "canal", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_outbox_status_disponivel",
                schema: "public",
                table: "outbox_eventos",
                columns: new[] { "status", "disponivel_em" },
                filter: "status IN ('pendente','falha')");

            migrationBuilder.CreateIndex(
                name: "uk_outbox_idempotency",
                schema: "public",
                table: "outbox_eventos",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_servicos_ativo",
                schema: "public",
                table: "servicos",
                column: "ativo",
                filter: "ativo = true");

            migrationBuilder.CreateIndex(
                name: "uk_servicos_nome",
                schema: "public",
                table: "servicos",
                column: "nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uk_pref_usuario_id",
                schema: "public",
                table: "usuario_preferencias",
                column: "usuario_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_sessoes_expira_em",
                schema: "public",
                table: "usuario_sessoes",
                column: "expira_em");

            migrationBuilder.CreateIndex(
                name: "idx_sessoes_revogado_em",
                schema: "public",
                table: "usuario_sessoes",
                column: "revogado_em",
                filter: "revogado_em IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_sessoes_usuario_id",
                schema: "public",
                table: "usuario_sessoes",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "idx_usuarios_ativo",
                schema: "public",
                table: "usuarios",
                column: "ativo",
                filter: "ativo = false");

            migrationBuilder.CreateIndex(
                name: "uk_usuarios_email",
                schema: "public",
                table: "usuarios",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_veiculos_cliente_id",
                schema: "public",
                table: "veiculos",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "uk_veiculos_placa",
                schema: "public",
                table: "veiculos",
                column: "placa",
                unique: true);

            // RN011 — bloqueio em banco de conflito global de veículo por janela.
            // Janela meio-aberta [inicio, fim); ignora cancelados/finalizados.
            migrationBuilder.Sql(@"
ALTER TABLE public.agendamentos
ADD CONSTRAINT ex_ag_veiculo_janela
EXCLUDE USING gist (
    veiculo_id WITH =,
    tstzrange(inicio, fim, '[)') WITH &&
)
WHERE (status = 'agendado');");

            // Unique lógico do feature_flags — COALESCE para tratar NULL = NULL como conflito.
            migrationBuilder.Sql(@"
CREATE UNIQUE INDEX uk_flag_nome_ambiente_filial
ON public.feature_flags (
    nome,
    ambiente,
    COALESCE(filial_id, '00000000-0000-0000-0000-000000000000'::uuid)
);");

            // Seed técnico (DB001 §05) — usa UUIDs fixos para idempotência.
            var agora = DateTime.UtcNow;
            var adminId = new Guid("00000000-0000-0000-0000-000000000001");
            var matrizId = new Guid("00000000-0000-0000-0000-000000000010");
            var servicoSimples = new Guid("00000000-0000-0000-0000-000000000100");
            var servicoCompleto = new Guid("00000000-0000-0000-0000-000000000101");
            var servicoEnceramento = new Guid("00000000-0000-0000-0000-000000000102");
            var preferenciaAdmin = new Guid("00000000-0000-0000-0000-000000000200");

            // Hash Argon2id em runtime — lê env CARWASH_SEED_ADMIN_PASSWORD (obrigatória).
            var senhaHash = SeedPasswordResolver.ResolveAdminArgon2idHash();

            migrationBuilder.InsertData(
                schema: "public",
                table: "usuarios",
                columns: new[] { "id", "nome", "email", "senha_hash", "perfil", "ativo", "criado_em", "atualizado_em" },
                values: new object[] { adminId, "Administrador", "admin@carwash.local", senhaHash, "ADMIN", true, agora, agora });

            migrationBuilder.InsertData(
                schema: "public",
                table: "filiais",
                columns: new[] { "id", "nome", "ativa", "celulas_ativas", "timezone", "criado_em", "atualizado_em" },
                values: new object[] { matrizId, "Matriz", true, 4, "America/Sao_Paulo", agora, agora });

            migrationBuilder.InsertData(
                schema: "public",
                table: "servicos",
                columns: new[] { "id", "nome", "preco", "duracao_min", "ativo", "criado_em", "atualizado_em" },
                values: new object[,]
                {
                    { servicoSimples, "Lavagem Simples", 30.00m, 30, true, agora, agora },
                    { servicoCompleto, "Lavagem Completa", 60.00m, 60, true, agora, agora },
                    { servicoEnceramento, "Enceramento", 45.00m, 45, true, agora, agora },
                });

            migrationBuilder.InsertData(
                schema: "public",
                table: "usuario_preferencias",
                columns: new[] { "id", "usuario_id", "tema", "atualizado_em" },
                values: new object[] { preferenciaAdmin, adminId, "claro", agora });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverter o que foi adicionado via SQL puro antes do drop das tabelas.
            migrationBuilder.Sql("DROP INDEX IF EXISTS public.uk_flag_nome_ambiente_filial;");
            migrationBuilder.Sql("ALTER TABLE IF EXISTS public.agendamentos DROP CONSTRAINT IF EXISTS ex_ag_veiculo_janela;");

            migrationBuilder.DropTable(
                name: "agendamento_historico",
                schema: "public");

            migrationBuilder.DropTable(
                name: "agendamento_itens",
                schema: "public");

            migrationBuilder.DropTable(
                name: "audit_logs",
                schema: "public");

            migrationBuilder.DropTable(
                name: "feature_flags",
                schema: "public");

            migrationBuilder.DropTable(
                name: "notificacoes",
                schema: "public");

            migrationBuilder.DropTable(
                name: "outbox_eventos",
                schema: "public");

            migrationBuilder.DropTable(
                name: "usuario_preferencias",
                schema: "public");

            migrationBuilder.DropTable(
                name: "usuario_sessoes",
                schema: "public");

            migrationBuilder.DropTable(
                name: "servicos",
                schema: "public");

            migrationBuilder.DropTable(
                name: "agendamentos",
                schema: "public");

            migrationBuilder.DropTable(
                name: "usuarios",
                schema: "public");

            migrationBuilder.DropTable(
                name: "filiais",
                schema: "public");

            migrationBuilder.DropTable(
                name: "filiados",
                schema: "public");

            migrationBuilder.DropTable(
                name: "veiculos",
                schema: "public");

            migrationBuilder.DropTable(
                name: "clientes",
                schema: "public");

            // btree_gist é mantida — pode haver outros consumidores no schema. Em ambiente
            // dedicado pode-se executar manualmente: DROP EXTENSION IF EXISTS btree_gist;
        }
    }
}
