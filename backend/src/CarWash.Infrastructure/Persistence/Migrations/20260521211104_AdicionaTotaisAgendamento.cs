using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarWash.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Adiciona os totais denormalizados do agendamento (RF020 / RN006):
    /// <c>duracao_total_min</c> e <c>valor_total</c>, com CHECKs de não-negatividade.
    /// O bloqueio de conflito de janela (RN011) é garantido pela constraint EXCLUDE
    /// <c>ex_ag_veiculo_janela</c> já criada na <c>InitialSchema</c> — não há nada
    /// de conflito a adicionar aqui.
    /// </summary>
    /// <inheritdoc />
    public partial class AdicionaTotaisAgendamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.AddColumn<int>(
                name: "duracao_total_min",
                schema: "public",
                table: "agendamentos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "valor_total",
                schema: "public",
                table: "agendamentos",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddCheckConstraint(
                name: "ck_ag_duracao_total",
                schema: "public",
                table: "agendamentos",
                sql: "duracao_total_min >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_ag_valor_total",
                schema: "public",
                table: "agendamentos",
                sql: "valor_total >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.DropCheckConstraint(
                name: "ck_ag_duracao_total",
                schema: "public",
                table: "agendamentos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_ag_valor_total",
                schema: "public",
                table: "agendamentos");

            migrationBuilder.DropColumn(
                name: "duracao_total_min",
                schema: "public",
                table: "agendamentos");

            migrationBuilder.DropColumn(
                name: "valor_total",
                schema: "public",
                table: "agendamentos");
        }
    }
}
