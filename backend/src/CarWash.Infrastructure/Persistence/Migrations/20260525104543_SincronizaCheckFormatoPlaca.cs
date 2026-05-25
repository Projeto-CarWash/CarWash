using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarWash.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SincronizaCheckFormatoPlaca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_servicos_duracao",
                schema: "public",
                table: "servicos");

            migrationBuilder.AddCheckConstraint(
                name: "ck_veiculos_placa_formato",
                schema: "public",
                table: "veiculos",
                sql: "placa ~ '^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_servicos_duracao_max",
                schema: "public",
                table: "servicos",
                sql: "duracao_min <= 1440");

            migrationBuilder.AddCheckConstraint(
                name: "ck_servicos_duracao_positiva",
                schema: "public",
                table: "servicos",
                sql: "duracao_min > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_veiculos_placa_formato",
                schema: "public",
                table: "veiculos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_servicos_duracao_max",
                schema: "public",
                table: "servicos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_servicos_duracao_positiva",
                schema: "public",
                table: "servicos");

            migrationBuilder.AddCheckConstraint(
                name: "ck_servicos_duracao",
                schema: "public",
                table: "servicos",
                sql: "duracao_min > 0");
        }
    }
}
