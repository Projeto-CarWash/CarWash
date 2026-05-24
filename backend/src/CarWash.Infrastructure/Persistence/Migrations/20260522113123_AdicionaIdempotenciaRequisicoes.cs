using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarWash.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Cria a tabela <c>idempotencia_requisicoes</c> (RF015 / ADR 0004): registra
    /// as confirmações de agendamento para tornar duplo clique e retry de rede
    /// idempotentes. A UNIQUE <c>uq_idempotencia_key_escopo</c> é a defesa
    /// anti-race; o índice <c>ix_idempotencia_expira_em</c> apoia o job diário de
    /// limpeza (janela de validade de 24h).
    /// </summary>
    /// <inheritdoc />
    public partial class AdicionaIdempotenciaRequisicoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.CreateTable(
                name: "idempotencia_requisicoes",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<Guid>(type: "uuid", nullable: false),
                    escopo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payload_hash = table.Column<string>(type: "char(64)", fixedLength: true, nullable: false),
                    status_http = table.Column<int>(type: "integer", nullable: false),
                    resposta_json = table.Column<string>(type: "jsonb", nullable: false),
                    recurso_id = table.Column<Guid>(type: "uuid", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    expira_em = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_idempotencia_requisicoes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_idempotencia_expira_em",
                schema: "public",
                table: "idempotencia_requisicoes",
                column: "expira_em");

            migrationBuilder.CreateIndex(
                name: "uq_idempotencia_key_escopo",
                schema: "public",
                table: "idempotencia_requisicoes",
                columns: new[] { "idempotency_key", "escopo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.DropTable(
                name: "idempotencia_requisicoes",
                schema: "public");
        }
    }
}
