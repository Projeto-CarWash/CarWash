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

            // NOT NULL sem default: RF024 exige responsável em todo agendamento.
            // Um default Guid.Empty seria um valor de FK inválido (não existe
            // responsável com id zero) e quebraria o re-add da fk_ag_responsavel
            // em bases com linhas legadas NULL. O ambiente parte de base limpa; se
            // houver dados legados, faça o backfill explícito antes desta migration.
            migrationBuilder.AlterColumn<Guid>(
                name: "responsavel_id",
                schema: "public",
                table: "agendamentos",
                type: "uuid",
                nullable: false,
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
