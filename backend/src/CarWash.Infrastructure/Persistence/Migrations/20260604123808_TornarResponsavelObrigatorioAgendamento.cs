using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarWash.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TornarResponsavelObrigatorioAgendamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ag_responsavel",
                schema: "public",
                table: "agendamentos");

            migrationBuilder.AlterColumn<Guid>(
                name: "responsavel_id",
                schema: "public",
                table: "agendamentos",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_ag_responsavel",
                schema: "public",
                table: "agendamentos",
                column: "responsavel_id",
                principalSchema: "public",
                principalTable: "responsaveis",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ag_responsavel",
                schema: "public",
                table: "agendamentos");

            migrationBuilder.AlterColumn<Guid>(
                name: "responsavel_id",
                schema: "public",
                table: "agendamentos",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "fk_ag_responsavel",
                schema: "public",
                table: "agendamentos",
                column: "responsavel_id",
                principalSchema: "public",
                principalTable: "filiados",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
