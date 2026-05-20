using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarWash.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClientesVeiculos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clientes",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clientes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "veiculos",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    placa = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    modelo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    fabricante = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    cor = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_veiculos", x => x.id);
                    table.ForeignKey(
                        name: "fk_veiculos_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalSchema: "public",
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_veiculos_cliente_id",
                schema: "public",
                table: "veiculos",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_veiculos_placa",
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

            migrationBuilder.DropTable(
                name: "clientes",
                schema: "public");
        }
    }
}
