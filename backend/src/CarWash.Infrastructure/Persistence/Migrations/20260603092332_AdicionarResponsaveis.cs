using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarWash.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarResponsaveis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "responsaveis",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_titular_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    documento = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    telefone = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    grau_vinculo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criado_em = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    atualizado_em = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_responsaveis", x => x.id);
                    table.CheckConstraint("ck_responsaveis_grau_vinculo", "grau_vinculo IN ('RESPONSAVEL_FINANCEIRO','RESPONSAVEL_LEGAL','PROCURADOR','CONJUGE','PAI_MAE','OUTRO')");
                    table.ForeignKey(
                        name: "fk_responsaveis_cliente_titular",
                        column: x => x.cliente_titular_id,
                        principalSchema: "public",
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_responsaveis_cliente_titular_id",
                schema: "public",
                table: "responsaveis",
                column: "cliente_titular_id");

            migrationBuilder.CreateIndex(
                name: "uk_responsaveis_documento",
                schema: "public",
                table: "responsaveis",
                column: "documento",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "responsaveis",
                schema: "public");
        }
    }
}
