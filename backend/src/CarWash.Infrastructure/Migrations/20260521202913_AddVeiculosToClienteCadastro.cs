using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarWash.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVeiculosToClienteCadastro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "veiculos",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    placa = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    modelo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    fabricante = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    cor = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ano = table.Column<int>(type: "integer", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    cliente_id1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_veiculos", x => x.id);
                    table.CheckConstraint("ck_veiculos_placa_formato", "placa ~ '^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$'");
                    table.ForeignKey(
                        name: "fk_veiculos_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalSchema: "public",
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_veiculos_clientes_cliente_id1",
                        column: x => x.cliente_id1,
                        principalSchema: "public",
                        principalTable: "clientes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "idx_veiculos_cliente_id",
                schema: "public",
                table: "veiculos",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_veiculos_cliente_id1",
                schema: "public",
                table: "veiculos",
                column: "cliente_id1");

            migrationBuilder.CreateIndex(
                name: "uk_veiculos_placa",
                schema: "public",
                table: "veiculos",
                column: "placa",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "veiculos",
                schema: "public");
        }
    }
}
