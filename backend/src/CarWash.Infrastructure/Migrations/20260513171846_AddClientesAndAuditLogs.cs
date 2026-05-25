using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarWash.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClientesAndAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "audit_logs",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    evento = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    entidade = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    entidade_id = table.Column<Guid>(type: "uuid", nullable: true),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    dados = table.Column<string>(type: "jsonb", nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clientes",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    cpf = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    cnpj = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    telefone = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    celular = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    endereco = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    observacoes = table.Column<string>(type: "text", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clientes", x => x.id);
                    table.CheckConstraint("ck_clientes_celular_somente_digitos", "celular IS NULL OR celular ~ '^[0-9]{11}$'");
                    table.CheckConstraint("ck_clientes_cpf_ou_cnpj", "cpf IS NOT NULL OR cnpj IS NOT NULL");
                    table.CheckConstraint("ck_clientes_telefone_somente_digitos", "telefone IS NULL OR telefone ~ '^[0-9]{10,11}$'");
                });

            migrationBuilder.CreateIndex(
                name: "idx_audit_correlation",
                schema: "public",
                table: "audit_logs",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "idx_audit_criado_em",
                schema: "public",
                table: "audit_logs",
                column: "criado_em");

            migrationBuilder.CreateIndex(
                name: "idx_audit_entidade",
                schema: "public",
                table: "audit_logs",
                column: "entidade");

            migrationBuilder.CreateIndex(
                name: "idx_audit_evento",
                schema: "public",
                table: "audit_logs",
                column: "evento");

            migrationBuilder.CreateIndex(
                name: "idx_clientes_email",
                schema: "public",
                table: "clientes",
                column: "email");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs",
                schema: "public");

            migrationBuilder.DropTable(
                name: "clientes",
                schema: "public");
        }
    }
}
